/*!*****************************************************************************************************
        KS ImGui version 1.1.0
        Copyright 2020, by Karbon Solutions

        This copy is licensed to Universiteit van Amsterdam

        License is granted under terms of the license agreement entered by the registed user.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
        INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
        PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
        LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT
        OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
        OTHER DEALINGS IN THE SOFTWARE.
*******************************************************************************************************/

interface Math { imul(a: number, b: number): number; }

namespace ks {
    declare const $: any;

    // https://stackoverflow.com/questions/4565112/javascript-how-to-find-out-if-the-user-browser-is-chrome/13348618#13348618
    let is_chromium = (<any>window).chrome;
    let is_opera = typeof (<any>window).opr !== "undefined";
    let is_ms_edge = window.navigator.userAgent.indexOf("Edge") >= 0;
    let is_internet_explorer = /MSIE 9/i.test(navigator.userAgent) || /MSIE 10/i.test(navigator.userAgent) ||
        /rv:11.0/i.test(navigator.userAgent);
    let is_microsoft = is_ms_edge || is_internet_explorer;
    let is_ios_chrome = window.navigator.userAgent.match("CriOS");
    let is_google_chrome;

    if (is_ios_chrome) {
        // is Google Chrome on IOS
    } else if (is_chromium !== null && typeof is_chromium !== "undefined" &&
        window.navigator.vendor === "Google Inc." && is_opera === false && is_ms_edge === false
    ) {
        is_google_chrome = true;
    }
    else { is_google_chrome = false; }


    if (!Math.imul) {
        Math.imul = function (a: number, b: number): number {
            b |= 0;
            let result = (a & 0x003fffff) * b;
            if (a & 0xffc00000) { result += (a & 0xffc00000) * b | 0; }
            return result | 0;
        };
    }

    export enum Item_Type {
        container,
        panel,
        column,
        combo,
        collapsing_header,
        selectable,
        text,
        button,
        input_text,
        input_text_area,
        input_password,
        input_number,
        input_date,
        input_file,
        checkbox,
        radio_button,
        switch_button,
        modal,
        modal_footer,
        row,
        group,
        table,
        table_head,
        table_body,
        table_row,
        table_cell,
        fleeting,
        anchor,
        nav,
        navbar,
        form,
        alert,
        ordered_list,
        unordered_list,
        list_item,
        progress_bar,
        spinner,
        set_timeout,
        set_interval,

        max_types,
    }

    export enum Sort_Order {
        none = 0,
        asc = 1,
        desc = -1,
    }

    export enum KS_Icon {
        // Used in collapsing header
        collapsed,
        expanded,

        // Used in table headers
        sort,
        sort_asc,
        sort_desc,
    }

    export let ks_icons = [
        'fa fa-chevron-up',
        'fa fa-chevron-down',

        'fa fa-sort',
        'fa fa-sort-asc',
        'fa fa-sort-desc',
    ];

    class Tree_Item {
        el: any;
        parent: Tree_Item;
        i_child: number;
        child_count: number;
    }

    class Tree_Item_Buffer {
        items: Tree_Item[] = [];
        length = 0;

        constructor() {
            for (let i = 0; i < 1000; ++i) {
                this.items.push({ el: undefined, parent: undefined, i_child: -1, child_count: 0 });
            }
        }

        push(el): Tree_Item {
            if (this.length === this.items.length) {
                let push_count = Math.ceil(this.items.length * 1.5) - this.items.length;
                for (let i = 0; i < push_count; ++i) {
                    this.items.push({ el: undefined, parent: undefined, i_child: -1, child_count: 0 });
                }
            }

            let item = this.items[this.length++];
            item.el = el;
            item.parent = undefined;
            item.i_child = -1;
            item.child_count = 0;
            return item;
        }

        push_child(parent: Tree_Item, el): Tree_Item {
            let item = this.push(el);
            item.parent = parent;
            item.i_child = parent.child_count++;
            return item;
        }

        clear(i_begin: number) {
            this.length = i_begin;
        }
    }

    class State_Stack {
        items: { current: any, parent: any, form: any, modal: any }[] = [];
        length = 0;

        constructor() {
            for (let i = 0; i < 100; ++i) {
                this.items.push({ current: undefined, parent: undefined, form: undefined, modal: undefined });
            }
        }

        push(current, parent, form, modal) {
            if (this.length === this.items.length) {
                let push_count = Math.ceil(this.items.length * 1.5) - this.items.length;
                for (let i = 0; i < push_count; ++i) {
                    this.items.push({ current: undefined, parent: undefined, form: undefined, modal: undefined });
                }
            }

            let item = this.items[this.length++];
            item.current = current;
            item.parent = parent;
            item.form = form;
            item.modal = modal;
        }

        pop(): { current: any, parent: any, form: any, modal: any } {
            console.assert(this.length > 0);
            return this.items[--this.length];
        }
    }

    let container;
    let item_current;
    let item_current_parent;
    let item_current_form;
    let item_current_modal;
    let state_stack = new State_Stack();
    let item_map = {};
    let id_chain = [0];
    let tree_item: Tree_Item;
    let tree_item_map = {};
    let tree_item_buffer = new Tree_Item_Buffer();
    let is_refresh = false;
    let pass_id = 0;
    let fleeting_id = Item_Type.max_types;
    let next_item_class_name;
    let next_item_this;
    let main_proc;
    let local_persists = {};

    function add_throttled_event_listener(target: EventTarget, type: string, proc: (ev: Event) => void) {
        let request_id = 0;
        target.addEventListener(type, (ev) => {
            if (request_id) { window.cancelAnimationFrame(request_id); }

            request_id = window.requestAnimationFrame(() => {
                proc(ev);
                request_id = 0;
            });
        });
    }

    function on_load(ev) {
        container = document.body;
        item_current = container;
        item_current_parent = container;

        item_info_add(container, 0, 0, '', main_proc);

        ks.refresh(container, true);
    }

    function on_popstate(ev) {
        refresh(container);
    }

    export function navigate_to(title: string, url: string) {
        window.history.pushState(undefined, title, url);
        if (is_refresh) { navigate_after_refresh = true; }
        else { refresh(container); }
    }

    export function run(proc_main) {
        main_proc = proc_main;

        window.removeEventListener('load', on_load);
        window.addEventListener('load', on_load);

        // For some reason adding an unload event fixed FireFox back button not properly refreshing the page.
        window.removeEventListener('unload', no_op);
        window.addEventListener('unload', no_op);

        window.removeEventListener('popstate', on_popstate);
        window.addEventListener('popstate', on_popstate);
    }

    export function row(id: string, children_proc): HTMLElement {
        return item_container(id, Item_Type.row, 'DIV', children_proc, function () {
            set_class_name(this, item_current_form ? 'form-row' : 'row');
        });
    }

    export function column(id: string, size: number | string, children_proc): HTMLElement {
        return item_container(id, Item_Type.column, 'DIV', children_proc, function () {
            set_class_name(this, !size ? 'col' : 'col-' + size);
        });
    }

    export function combo(label: string, children_proc): HTMLSelectElement {
        let do_item = function (): HTMLSelectElement {
            let class_valid = !next_input_validation ? '' : (next_input_validation.is_valid ? ' is-valid' : ' is-invalid');
            let id = hash_str(label, id_chain[id_chain.length - 1]);
            let existing = item_existing(id, Item_Type.combo);
            if (existing) {
                set_class_name(existing, 'custom-select' + class_valid);
                push_set_id(existing._ks_info.id);
                return temp_switch_parent_apply(existing, children_proc);
            }

            let el = <any>document.createElement('SELECT');
            let info = item_info_add(el, Item_Type.combo, id, label_extract(label), children_proc);
            push_set_id(info.id);

            set_class_name(el, 'custom-select' + class_valid);
            // Since onclick on <option> elements is not supported in chrome and mobile? browsers 
            // and it is unclear when it is/isn't supported, we always use this workaround.
            {
                el.onchange = function (ev) {
                    let value = el.value;
                    for (let i = 0; i < el.children.length; ++i) {
                        let child = el.children[i];
                        if (child._ks_info && child._ks_info.option_on_click && child.value === value) {
                            child._ks_info.option_on_click();
                            break;
                        }
                    }
                };
            }
            return temp_switch_parent_apply(el, children_proc, true);
        }

        // set_next_item stuff might not behave as user would expect, because it applies to
        // the group element and not the input element if used inside a form.
        // User can however then manually create the groups to avoid this.
        if (item_current_form) {
            let is_row = item_current_parent._ks_info && item_current_parent._ks_info.type === Item_Type.row;
            if (is_row && item_current_parent.parentElement === item_current_form || item_current_parent === item_current_form) {
                set_next_item_this(item_current_parent);
                let el;
                group(label, is_row ? 'form-group col' : 'form-group', function () {
                    el = do_item.call(this);
                    consume_next_input_validation();
                });
                return el;
            } else {
                let el = do_item.call(item_current_parent);
                consume_next_input_validation();
                return el;
            }
        }
        return do_item.call(item_current_parent);
    }

    export function collapsing_header(label: string, initially_open: boolean, children_proc): HTMLElement {
        ks.set_next_item_class_name('card ' + (next_item_class_name || ''));
        return item_container(label, Item_Type.collapsing_header, 'DIV', function () {
            let info = this._ks_info;
            let is_new = !info.el_collapse;
            if (is_new) { info.collapsed = !initially_open; }

            ks.set_next_item_class_name('d-flex flex-row py-2');
            let header = ks.card_header('header', function () {
                ks.text(label, 'flex-grow-1');
                ks.group('toggle', '', function () {
                    ks.set_next_item_class_name('text-muted');
                    ks.icon(!info.collapsed ? ks_icons[KS_Icon.collapsed] : ks_icons[KS_Icon.expanded]);
                });
            });
            header.style.cursor = 'pointer';
            ks.is_item_clicked(function () {
                info.collapsed = !info.collapsed;
                $(info.el_collapse).collapse(info.collapsed ? 'hide' : 'show');
                ks.refresh(header);
            });

            info.el_collapse = ks.group('collapse', 'collapse', children_proc);
            if (is_new) { $(info.el_collapse).collapse({ toggle: initially_open }); }
        });
    }

    export function open_popup(id_label: string) {
        let id = hash_str(id_label, id_chain[id_chain.length - 1]);
        let existing = item_existing(id, Item_Type.modal);
        if (existing && existing._ks_info.type === Item_Type.modal) {
            $(existing).modal('show');
        } else {
            console.error('Could not find modal \'' + id_label + '\'');
        }
    }

    export function close_current_popup() {
        if (item_current_modal) { $(item_current_modal).modal('hide'); }
        else { console.error('Could not find any current popup, make sure this method is called inside a popup and not in some asynchronous method like setTimeout or a HTTP request callback.'); }
    }

    export function close_popup(id_label: string) {
        let id = hash_str(id_label, id_chain[id_chain.length - 1]);
        let existing = item_existing(id, Item_Type.modal);
        if (existing && existing._ks_info.type === Item_Type.modal) {
            $(existing).modal('hide');
        } else {
            console.error('Could not find modal \'' + id_label + '\'');
        }
    }

    export enum Modal_Size {
        default = 0,
        small,
        large,
        extra_large
    }

    let next_modal_size = Modal_Size.default;

    export function set_next_modal_size(size: Modal_Size) {
        next_modal_size = size;
    }

    export function popup_modal(id_label: string, children_proc,
        hide_close = false, backdrop_cancel = false, keyboard_cancel = false): HTMLElement {
        let size_class: string;
        switch (next_modal_size) {
            case Modal_Size.small:
                size_class = ' modal-sm';
                break;
            case Modal_Size.large:
                size_class = ' modal-lg';
                break;
            case Modal_Size.extra_large:
                size_class = ' modal-xl';
                break;
            default:
                size_class = '';
                break;
        }
        next_modal_size = Modal_Size.default;

        let id = hash_str(id_label, id_chain[id_chain.length - 1]);
        let existing = item_existing(id, Item_Type.modal);
        let proc;
        let label: string;

        if (existing && existing._ks_info.pass_id === pass_id) {
            proc = children_proc;
        } else {
            label = label_extract(id_label);
            proc = function () {
                let prev_modal = item_current_modal;
                item_current_modal = this;

                if (label !== '' || !hide_close) {
                    (<any>modal_header(function () {
                        if (label !== '') { h5(label, 'modal-title'); }
                    }))._ks_info.pass_id = pass_id - 1; // make sure user can override class name
                }

                children_proc.apply(this);

                // Do this in a seperate pass so that user code for headers come before the close button
                if (!hide_close) {
                    modal_header(function () {
                        set_next_item_class_name('close');
                        button_container('##close', function () {
                            let x = group('x', '', no_op);
                            set_inner_text(x, '×');
                            x.setAttribute('aria-hidden', 'true');
                        }, '').setAttribute('aria-label', 'Close');
                        is_item_clicked(close_current_popup);
                    });
                }

                item_current_modal = prev_modal;
            };
        }

        if (existing) {
            set_class_name(existing, 'modal fade');
            push_set_id(existing._ks_info.id);
            if (existing._ks_info.pass_id !== pass_id) {
                set_class_name(existing._ks_info.el_dlg, 'modal-dialog' + size_class);
            }
            return temp_switch_parent_apply(existing, proc, false, undefined, existing);
        }

        let el = document.createElement('DIV');
        let info = item_info_add(el, Item_Type.modal, id, label, proc);
        push_set_id(info.id);

        set_class_name(el, 'modal fade');
        el.tabIndex = -1;
        el.setAttribute('role', 'dialog');

        let el_dlg = document.createElement('DIV');
        set_class_name(el_dlg, 'modal-dialog' + size_class);
        el_dlg.setAttribute('role', 'document');
        info.el_dlg = el_dlg;

        let el_content = document.createElement('DIV');
        el_content.className = 'modal-content';

        el.appendChild(el_dlg);
        el_dlg.appendChild(el_content);

        info.el_append = el_content;

        $(el).modal({
            keyboard: !!keyboard_cancel,
            show: false,
            backdrop: backdrop_cancel ? true : 'static',
        });

        return temp_switch_parent_apply(el, proc, true, undefined, el);
    }

    export function modal_header(children_proc: Function): HTMLElement {
        let popup = item_current_modal;
        if (!popup || popup._ks_info.type !== Item_Type.modal) {
            console.error('modal_header() should be called inside a modal');
            return;
        }

        let prev_tree_item = tree_item;
        tree_item = tree_item_map[popup._ks_info.id];
        // If there is no tree_item, an inner child of the modal is being refreshed
        // in which case we should not be doing anything here
        if (tree_item) {
            push_state(item_current, popup, item_current_form, item_current_modal);
            push_set_id(popup._ks_info.id);

            group('header', 'modal-header', children_proc);

            pop_id();
            pop_state();
        }
        tree_item = prev_tree_item;

        return item_existing(hash_str('header', popup._ks_info.id), Item_Type.group);
    }

    export function modal_body(children_proc: Function): HTMLElement {
        let popup = item_current_modal;
        if (!popup || popup._ks_info.type !== Item_Type.modal) {
            console.error('modal_body() should be called inside a modal');
            return;
        }

        let prev_tree_item = tree_item;
        tree_item = tree_item_map[popup._ks_info.id];
        // If there is no tree_item, an inner child of the modal is being refreshed
        // in which case we should not be doing anything here
        if (tree_item) {
            push_state(item_current, popup, item_current_form, item_current_modal);
            push_set_id(popup._ks_info.id);

            group('body', 'modal-body', children_proc);

            pop_id();
            pop_state();
        }
        tree_item = prev_tree_item;

        return item_existing(hash_str('body', popup._ks_info.id), Item_Type.group);
    }

    export function modal_footer(children_proc: Function): HTMLElement {
        let popup = item_current_modal;
        if (!popup || popup._ks_info.type !== Item_Type.modal) {
            console.error('modal_footer() should be called inside a modal');
            return;
        }

        let prev_tree_item = tree_item;
        tree_item = tree_item_map[popup._ks_info.id];
        // If there is no tree_item, an inner child of the modal is being refreshed
        // in which case we should not be doing anything here
        if (tree_item) {
            push_state(item_current, popup, item_current_form, item_current_modal);
            push_set_id(popup._ks_info.id);

            group('footer', 'modal-footer', children_proc);

            pop_id();
            pop_state();
        }
        tree_item = prev_tree_item;

        return item_existing(hash_str('footer', popup._ks_info.id), Item_Type.group);
    }

    export function table(id: string, children_proc, proc_sort?: (i_head: number, order: Sort_Order) => void): HTMLTableElement {
        return item_container(id, Item_Type.table, 'TABLE', children_proc, function (el) {
            set_class_name(el, 'table table-hover');
            let info = el._ks_info;
            info.proc_sort = proc_sort;
            if (info.pass_id !== pass_id) {
                info.i_table_row = 0;
                info.i_table_cell = 0;
            }
        });
    }

    export function table_trigger_sort(id_or_element: string | HTMLTableElement, refresh_table = true) {
        if (typeof id_or_element === 'string') {
            let id = hash_str(id_or_element, id_chain[id_chain.length - 1]);
            let existing = item_existing(id, Item_Type.table)
            if (existing) {
                let info = existing._ks_info;
                info.proc_sort.apply(info.this, [info.i_sort_head, info.sort_order]);
                if (refresh_table) { refresh(existing); }
            } else {
                console.error('Could not find table with id: ', id_or_element);
            }
        } else {
            let info = (<any>id_or_element)._ks_info;
            if (!info || info.type !== Item_Type.table) {
                console.error('Element is not a known table: ', id_or_element);
            } else {
                info.proc_sort.apply(info.this, [info.i_sort_head, info.sort_order]);
                if (refresh_table) { refresh(id_or_element); }
            }
        }
    }

    export function table_head(children_proc: Function): HTMLElement {
        if (item_current_parent._ks_info.type !== Item_Type.table) {
            console.error('table_head() should be called inside a table');
            return;
        }

        return item_container('head', Item_Type.table_head, 'THEAD', children_proc, function () {
            let info = this._ks_info;
            let info_table = item_current_parent._ks_info;
            if (info.pass_id !== pass_id) { info_table.i_table_row = 0; }
            set_class_name(this);
            info.el_table = item_current_parent;
            info.is_table_head = true;
        });
    }

    export function table_body(children_proc: Function): HTMLElement {
        if (item_current_parent._ks_info.type !== Item_Type.table) {
            console.error('table_body() should be called inside a table');
            return;
        }

        return item_container('body', Item_Type.table_body, 'TBODY', children_proc, function () {
            set_class_name(this);
            this._ks_info.el_table = item_current_parent;
        });
    }

    export function table_row(children_proc: Function): HTMLElement {
        let info_parent = item_current_parent._ks_info;
        if (info_parent.type !== Item_Type.table_head && info_parent.type !== Item_Type.table_body) {
            console.error('table_row() should be called inside table_head() or table_body()');
            return;
        }

        let table = info_parent.el_table;
        let info_table = table._ks_info;
        console.assert(table._ks_info.type === Item_Type.table);

        // TODO: currently only allows for a single header row, should we support multiple?
        let row_nr = info_parent.type === Item_Type.table_head ? 1 : 1 + (++info_table.i_table_row);
        let id = hash_fleeting(row_nr, id_chain[id_chain.length - 1]);
        let existing = item_existing(id, Item_Type.table_row);
        if (existing) {
            let info = existing._ks_info;
            if (info.pass_id !== pass_id) { info.i_table_cell = 0; }
            set_class_name(existing);
            info.el_table = table;
            info.is_head_row = !!item_current_parent._ks_info.is_table_head;
            push_set_id(existing._ks_info.id);
            return temp_switch_parent_apply(existing, children_proc);
        }

        let el = document.createElement('TR');
        let info = item_info_add(el, Item_Type.table_row, id, '', children_proc);
        info.i_table_cell = 0;
        info.el_table = table;
        info.is_head_row = !!item_current_parent._ks_info.is_table_head;
        set_class_name(el);
        push_set_id(id);
        return temp_switch_parent_apply(el, children_proc, true);
    }

    export function table_cell(str_or_children_proc: string | Function, initial_sort_order?: Sort_Order): HTMLTableCellElement {
        let info_parent = item_current_parent._ks_info;
        if (info_parent.type !== Item_Type.table_row) {
            console.error('table_cell() should be called inside table_row()');
            return;
        }

        const is_head = item_current_parent._ks_info.is_head_row;
        const row = item_current_parent;
        const table = item_current_parent._ks_info.el_table;

        console.assert(table._ks_info.type === Item_Type.table);

        if (is_head) { return table_cell_header(str_or_children_proc, initial_sort_order); }

        console.assert(initial_sort_order === undefined, 'Only cells in a table head should have a sort order.');

        let i_cell = row._ks_info.i_table_cell++;

        if (typeof str_or_children_proc !== 'function') {
            let el = recycle_set_current('TD');
            set_class_name(el);
            set_inner_text(el, str_or_children_proc);
            return el;
        }

        let id = hash_fleeting(i_cell, id_chain[id_chain.length - 1]);
        let existing = item_existing(id, Item_Type.table_cell);
        if (existing) {
            set_class_name(existing);
            push_set_id(existing._ks_info.id);
            return temp_switch_parent_apply(existing, str_or_children_proc);
        }

        let el = document.createElement('TD');
        item_info_add(el, Item_Type.table_cell, id, '', str_or_children_proc);
        set_class_name(el);
        push_set_id(id);
        return temp_switch_parent_apply(el, str_or_children_proc, true);
    }

    export function table_cell_header(str_or_children_proc: string | Function, initial_sort_order?: Sort_Order): HTMLTableHeaderCellElement {
        let info_parent = item_current_parent._ks_info;
        if (info_parent.type !== Item_Type.table_row) {
            console.error('table_cell_header() should be called inside table_row()');
            return;
        }

        const row = item_current_parent;
        const table = item_current_parent._ks_info.el_table;
        const info_table = table._ks_info;

        console.assert(table._ks_info.type === Item_Type.table);
        if (initial_sort_order !== undefined && !info_table.proc_sort) {
            console.warn('Initial sort order provided, but the table has no associated sort procedure.');
        }

        let i_cell = row._ks_info.i_table_cell++;
        let id = hash_fleeting(i_cell, id_chain[id_chain.length - 1]);

        let proc = function () {
            let info = this._ks_info;
            if (info_table.proc_sort && info.el_table_sort_icon) {
                is_item_clicked(function (i_head) {
                    for (let i = 0; i < row.children.length; ++i) {
                        let child = row.children[i];
                        if (i !== i_head && child._ks_info && child._ks_info.el_table_sort_icon) {
                            child._ks_info.sort_order = Sort_Order.none;
                            set_class_name(child._ks_info.el_table_sort_icon, ks_icons[KS_Icon.sort]);
                        }
                    }

                    if (info.sort_order === Sort_Order.none) {
                        info.sort_order = Sort_Order.asc;
                        set_class_name(info.el_table_sort_icon, ks_icons[KS_Icon.sort_asc]);
                    }
                    else if (info.sort_order === Sort_Order.asc) {
                        info.sort_order = Sort_Order.desc;
                        set_class_name(info.el_table_sort_icon, ks_icons[KS_Icon.sort_desc]);
                    }
                    else if (info.sort_order === Sort_Order.desc) {
                        info.sort_order = Sort_Order.asc;
                        set_class_name(info.el_table_sort_icon, ks_icons[KS_Icon.sort_asc]);
                    }

                    info_table.i_sort_head = i_head;
                    info_table.sort_order = info.sort_order;

                    if (!info_table.proc_sort.apply(table._ks_info.this, [i_head, info.sort_order])) {
                        refresh(table);
                    }
                }, i_cell);
            }

            if (typeof str_or_children_proc === 'function') {
                str_or_children_proc.apply(this);
            }
        };

        let existing = item_existing(id, Item_Type.table_cell);
        if (existing) {
            set_class_name(existing);
            if (typeof str_or_children_proc !== 'function') {
                set_inner_text(existing._ks_info.el_append, str_or_children_proc);
            } else {
                set_inner_text(existing._ks_info.el_append, '');
            }
            push_set_id(existing._ks_info.id);
            return temp_switch_parent_apply(existing, proc);
        }

        let el = <HTMLTableHeaderCellElement>document.createElement('TH');
        let info = item_info_add(el, Item_Type.table_cell, id, '', proc.bind(el));
        info.sort_order = initial_sort_order;
        push_set_id(info.id);

        el.scope = 'col';
        set_class_name(el);

        let el_flex = document.createElement('DIV');
        set_class_name(el_flex, 'd-flex flex-row align-items-center');
        let el_grow = document.createElement('DIV');
        set_class_name(el_grow, 'flex-grow-1');
        if (typeof str_or_children_proc !== 'function') {
            set_inner_text(el_grow, str_or_children_proc);
        } else {
            set_inner_text(el_grow, '');
        }

        el_flex.appendChild(el_grow);

        if (initial_sort_order !== undefined) {
            el.style.cursor = 'pointer';

            let el_icon_container = document.createElement('DIV');
            set_class_name(el_icon_container, 'ml-1');
            let el_icon = document.createElement('I');
            el_icon.setAttribute('aria-hidden', 'true');

            if (initial_sort_order !== Sort_Order.none && info_table.i_sort_head === undefined) {
                info_table.i_sort_head = i_cell;
                info_table.sort_order = initial_sort_order;
            }

            if (initial_sort_order === Sort_Order.none) { set_class_name(el_icon, ks_icons[KS_Icon.sort]); }
            else if (initial_sort_order === Sort_Order.asc) { set_class_name(el_icon, ks_icons[KS_Icon.sort_asc]); }
            else if (initial_sort_order === Sort_Order.desc) { set_class_name(el_icon, ks_icons[KS_Icon.sort_desc]); }

            el_icon_container.appendChild(el_icon);
            el_flex.appendChild(el_icon_container);

            info.el_table_sort_icon = el_icon;
        }

        el.appendChild(el_flex);

        info.el_append = el_grow;
        return temp_switch_parent_apply(el, proc, true);
    }

    export function anchor(id: string, href: string, children_proc?): HTMLAnchorElement {
        return item_container(id, Item_Type.anchor, 'A', children_proc || no_op, function (el) {
            el.href = href;
            set_inner_text(el, children_proc ? '' : el._ks_info.label);
            set_class_name(el);
        });
    }

    export function nav(id: string, class_name: string, children_proc: Function): HTMLElement {
        return item_container(id, Item_Type.nav, 'NAV', children_proc, function (el, is_new) {
            set_class_name(el, class_name);
        });
    }

    export function nav_bar(id: string, class_name: string, children_proc: Function): HTMLElement {
        return item_container(id, Item_Type.navbar, 'NAV', children_proc, function (el, is_new) {
            set_class_name(el, 'navbar ' + class_name);
        });
    }

    export function nav_item(id: string, is_active: boolean, href: string, children_proc?): HTMLElement {
        return item_container(id, Item_Type.navbar, 'LI', children_proc || no_op, function (el, is_new) {
            set_class_name(el, 'nav-item');
            if (!is_new) {
                let el_anchor = el._ks_info.el_append;
                el_anchor.href = href;
                set_inner_text(el_anchor, children_proc ? '' : el._ks_info.label);
                set_class_name(el_anchor, 'nav-link' + (is_active ? ' active' : ''));
                return;
            }

            let el_anchor = <HTMLAnchorElement>document.createElement('A');
            el_anchor.href = href;
            set_class_name(el_anchor, 'nav-link' + (is_active ? ' active' : ''));
            set_inner_text(el_anchor, children_proc ? '' : el._ks_info.label);

            el.appendChild(el_anchor);
            el._ks_info.el_append = el_anchor;
        });
    }

    export function form(id: string, action: string, is_inline: boolean, children_proc): HTMLFormElement {
        let el = item_container(id, Item_Type.form, 'FORM', function () {
            item_current_form = this;
            children_proc.apply(this);
        }, function (el, is_new) {
            set_class_name(el, is_inline ? 'form-inline' : undefined);
            if (is_new) {
                el._ks_info.show_validation = false;
                this.setAttribute('novalidate', true);
            }
        });
        if (!action) { el.action = 'javascript:void(0);'; }
        else { el.action = action; el.method = 'post'; }

        let current_stored = item_current;
        let parent_stored = item_current_parent;
        let modal_stored = item_current_modal;
        el.onsubmit = function (event) {
            push_state(current_stored, parent_stored, el, modal_stored);

            if (!el._ks_info.cancel_submission) {
                el._ks_info.form_is_submitted = true;
                el._ks_info.show_validation = true;
                refresh(el, true);
                el._ks_info.form_is_submitted = false;
            }

            if (el._ks_info.cancel_submission) {
                event.preventDefault();
            }
            el._ks_info.cancel_submission = false;

            pop_state();
        };
        el.onreset = function (event) {
            push_state(current_stored, parent_stored, el, modal_stored);

            el._ks_info.form_is_submitted = false;
            refresh(el, true);
            event.preventDefault();

            pop_state();
        };

        return el;
    }

    export function current_form_submitted() {
        if (!item_current_form) {
            console.error('current_form_submitted() should be called inside a form');
            return;
        }
        return !!item_current_form._ks_info.form_is_submitted;
    }

    export function cancel_current_form_submission() {
        if (!item_current_form) {
            console.error('cancel_current_form_submission() should be called inside a form');
            return;
        }
        item_current_form._ks_info.cancel_submission = true;
    }

    export function unsubmit_current_form() {
        if (!item_current_form) {
            console.error('unsubmit_current_form() should be called inside a form');
            return;
        }
        item_current_form._ks_info.form_is_submitted = false;
        item_current_form._ks_info.show_validation = false;
    }

    export function form_hide_validation(form: HTMLFormElement) {
        form._ks_info.show_validation = false;
    }

    export function submit_form(form: HTMLFormElement) {
        form._ks_info.cancel_submission = false;
        let event;
        if (typeof (Event) === 'function') {
            event = new Event('submit', { bubbles: true, cancelable: true });
        } else {
            event = document.createEvent('Event');
            event.initEvent('submit', true, true);
        }
        // TODO: Firefox says: Form submission via untrusted submit event is deprecated and will be removed at a future date.
        form.dispatchEvent(event);
    }

    export function alert_box(id: string, type: 'primary' | 'secondary' | 'success' | 'info' | 'warning' | 'danger' | 'light' | 'dark', hide_close: boolean, children_proc): HTMLElement {
        return item_container(id, Item_Type.alert, 'DIV', function () {
            if (!hide_close) {
                set_next_item_class_name('close');
                button_container('##close', function () {
                    let x = group('x', '', no_op);
                    set_inner_text(x, '×');
                    x.setAttribute('aria-hidden', 'true');
                }, '').setAttribute('aria-label', 'Close');
                is_item_clicked(function () { this.style.display = 'none'; });
            }

            children_proc.apply(this);
        }, function (el, is_new) {
            if (is_new) { el.setAttribute('role', 'alert'); }
            set_class_name(el, 'alert alert-' + type);
        });
    }

    export function progress_bar(id: string, label: string, val: number, max: number, bar_class_name: string): HTMLElement {
        return item_container(id, Item_Type.progress_bar, 'DIV', function () {
            let el: HTMLElement = recycle_set_current('DIV');
            el.style.width = (max === 0 ? 0 : Math.min(Math.max(0, val), max) / max * 100) + '%';
            el.setAttribute('aria-valuenow', val.toString());
            el.setAttribute('aria-valuemin', '0');
            el.setAttribute('aria-valuemax', max.toString());
            set_class_name(el, 'progress-bar ' + bar_class_name);
            set_inner_text(el, label);
        }, function (el) {
            set_class_name(el, 'progress');
        });
    }

    export function card(id: string, children_proc): HTMLElement {
        return group(id, 'card', children_proc);
    }

    export function card_header(id: string, children_proc): HTMLElement {
        return group(id, 'card-header', children_proc);
    }

    export function card_body(id: string, children_proc): HTMLElement {
        return group(id, 'card-body', children_proc);
    }

    export function card_footer(id: string, children_proc): HTMLElement {
        return group(id, 'card-footer', children_proc);
    }

    export function ordered_list(id: string, class_name: string, children_proc): HTMLOListElement {
        return item_container(id, Item_Type.ordered_list, 'OL', children_proc, function (el) {
            set_class_name(el, class_name);
        });
    }

    export function unordered_list(id: string, class_name: string, children_proc): HTMLUListElement {
        return item_container(id, Item_Type.unordered_list, 'UL', children_proc, function (el) {
            set_class_name(el, class_name);
        });
    }

    export function list_item(label: string, class_name: string, children_proc?: Function): HTMLUListElement {
        if (!children_proc) {
            let el = recycle_set_current('LI');
            set_class_name(el, class_name);
            set_inner_text(el, label_extract(label));
            return el;
        }

        return item_container(label, Item_Type.list_item, 'LI', children_proc, function (el) {
            set_class_name(el, class_name);
        });
    }

    export function group(id: string, class_name: string, children_proc): HTMLElement {
        return item_container(id, Item_Type.group, 'DIV', children_proc, function (el) {
            set_class_name(el, class_name);
        });
    }

    function item_container(id_label: string, type: Item_Type, tag_name: string, children_proc,
        el_created_proc?: (el: any, is_new: boolean) => void) {
        let id = hash_str(id_label, id_chain[id_chain.length - 1]);
        let existing = item_existing(id, type);
        if (existing) {
            if (id_label !== existing._ks_info.label) { existing._ks_info.label = label_extract(id_label); }
            if (el_created_proc) { el_created_proc.apply(existing, [existing, false]); }
            else { set_class_name(existing); }
            push_set_id(existing._ks_info.id);
            return temp_switch_parent_apply(existing, children_proc);
        }

        let el = document.createElement(tag_name);
        let info = item_info_add(el, type, id, label_extract(id_label), children_proc);
        if (el_created_proc) { el_created_proc.apply(el, [el, true]); }
        else { set_class_name(el); }
        push_set_id(info.id);

        return temp_switch_parent_apply(el, children_proc, true);
    }



    export function selectable(id_label: string, is_selected: boolean): HTMLOptionElement {
        let id = hash_str(id_label, id_chain[id_chain.length - 1]);
        let existing = item_existing(id, Item_Type.selectable);
        if (existing) {
            if (id_label !== existing._ks_info.label) { existing._ks_info.label = label_extract(id_label); }
            set_inner_text(existing, existing._ks_info.label);
            if (existing._ks_selected !== is_selected) {
                existing.selected = is_selected;
                existing._ks_selected = is_selected;
            }
            set_class_name(existing);
            return append_set_current(existing);
        }

        let el: any = document.createElement('OPTION');
        let label = label_extract(id_label);
        item_info_add(el, Item_Type.selectable, id, label);

        set_inner_text(el, label);
        set_class_name(el);
        el.selected = is_selected;
        el._ks_selected = is_selected;

        return append_set_current(el, true);
    }

    // label is used for screen readers
    export function spinner(id_label: string, class_name = 'spinner-border text-primary'): HTMLElement {
        let id = hash_str(id_label, id_chain[id_chain.length - 1]);
        let existing = item_existing(id, Item_Type.spinner);
        if (existing) {
            if (id_label !== existing._ks_info.label) { existing._ks_info.label = label_extract(id_label); }
            set_inner_text(existing._ks_info.el_screen_reader, existing._ks_info.label);
            set_class_name(existing, class_name);
            return append_set_current(existing);
        }

        let el: any = document.createElement('DIV');
        let label = label_extract(id_label);
        let info = item_info_add(el, Item_Type.spinner, id, label);

        el.setAttribute('role', 'status');
        set_class_name(el, class_name);

        let span = document.createElement('SPAN');
        span.className = 'sr-only';
        set_inner_text(span, label);
        info.el_screen_reader = span;
        el.appendChild(span);

        return append_set_current(el, true);
    }

    export function icon(class_name: string): HTMLElement {
        let el = recycle_set_current('I');
        set_class_name(el, class_name);
        el.setAttribute('aria-hidden', 'true');
        return el;
    }

    export function text(str: string, class_name?: string): HTMLElement {
        let el = recycle_set_current('DIV');
        set_class_name(el, class_name);
        set_inner_text(el, str);
        return el;
    }

    export function paragraph(str: string, class_name?: string): HTMLElement {
        let el = recycle_set_current('P');
        set_class_name(el, class_name);
        set_inner_text(el, str);
        return el;
    }

    export function h1(str: string, class_name?: string): HTMLElement {
        let el = recycle_set_current('H1');
        set_class_name(el, class_name);
        set_inner_text(el, str);
        return el;
    }

    export function h2(str: string, class_name?: string): HTMLElement {
        let el = recycle_set_current('H2');
        set_class_name(el, class_name);
        set_inner_text(el, str);
        return el;
    }

    export function h3(str: string, class_name?: string): HTMLElement {
        let el = recycle_set_current('H3');
        set_class_name(el, class_name);
        set_inner_text(el, str);
        return el;
    }

    export function h4(str: string, class_name?: string): HTMLElement {
        let el = recycle_set_current('H4');
        set_class_name(el, class_name);
        set_inner_text(el, str);
        return el;
    }

    export function h5(str: string, class_name?: string): HTMLElement {
        let el = recycle_set_current('H5');
        set_class_name(el, class_name);
        set_inner_text(el, str);
        return el;
    }

    export function h6(str: string, class_name?: string): HTMLElement {
        let el = recycle_set_current('H6');
        set_class_name(el, class_name);
        set_inner_text(el, str);
        return el;
    }

    export function image(src: string, width?: number, height?: number, class_name?: string, alt?: string): HTMLImageElement {
        let el: HTMLImageElement = recycle_set_current('IMG');
        el.src = src;
        if (width) { el.width = width; }
        if (height) { el.height = height; }
        set_class_name(el, class_name);
        if (alt) { el.alt = alt; }
        return el;
    }

    export function separator(): HTMLElement {
        let el = recycle_set_current('HR');
        set_class_name(el);
        return el;
    }

    export function new_line(): HTMLElement {
        let el = recycle_set_current('BR');
        set_class_name(el);
        return el;
    }

    export function button(id_label: string, proc_input, type = 'primary'): HTMLButtonElement {
        let class_name = type ? 'btn btn-' + type : undefined;
        let id = hash_str(id_label, id_chain[id_chain.length - 1]);
        let existing = item_existing(id, Item_Type.button);
        if (existing) {
            if (id_label !== existing._ks_info.label) { existing._ks_info.label = label_extract(id_label); }
            set_class_name(existing, class_name);
            set_inner_text(existing, existing._ks_info.label);
            append_set_current(existing);
            is_item_clicked(proc_input);
            return existing;
        }


        let el = <HTMLButtonElement>document.createElement('BUTTON');
        let label = label_extract(id_label);
        set_class_name(el, class_name);
        set_inner_text(el, label);
        item_info_add(el, Item_Type.button, id, label);
        append_set_current(el, true);
        is_item_clicked(proc_input);
        return el;
    }

    export function button_container(id: string, children_proc: Function, type = 'primary'): HTMLButtonElement {
        return item_container(id, Item_Type.group, 'BUTTON', children_proc, function (el) {
            let class_name = type ? 'btn btn-' + type : undefined;
            set_class_name(el, class_name);
        });
    }

    export function input_text(label: string, str: string, placeholder: string,
        proc_change: (val: string) => void): HTMLInputElement {
        return <HTMLInputElement>text_input('text', label, str, placeholder, proc_change);
    }

    export function input_text_area(label: string, str: string, placeholder:
        string, proc_change: (val: string) => void): HTMLTextAreaElement {
        return <HTMLTextAreaElement>text_input('text_area', label, str, placeholder, proc_change);
    }

    export function input_password(label: string, str: string, placeholder: string,
        proc_change: (val: string) => void): HTMLInputElement {
        return <HTMLInputElement>text_input('password', label, str, placeholder, proc_change);
    }

    export function input_number(label: string, number: number, placeholder: string | number, proc_change: (val: number) => void,
        min?: number, max?: number, step?: number): HTMLInputElement {
        let el = <HTMLInputElement>text_input('number', label, number, placeholder, proc_change);
        if (min !== undefined) { el.min = min.toString(); }
        if (max !== undefined) { el.max = max.toString(); }
        if (step !== undefined) { el.step = step.toString(); }
        return el;
    }

    export function input_date(label: string, str: string, placeholder: string,
        proc_change: (val: string) => void): HTMLInputElement {
        return <HTMLInputElement>text_input(!is_internet_explorer ? 'date' : 'text', label, str, placeholder, proc_change);
    }

    // placeholder and browse_label are ignored atm
    export function input_file(id: string, placeholder: string, proc_change: (files: FileList) => void,
        browse_label?: string): HTMLInputElement {
        return <HTMLInputElement>text_input('file', id, '', '', proc_change);
    }

    let next_input_validation;

    export function show_form_validation() {
        return item_current_form && item_current_form._ks_info.show_validation;
    }

    export function set_next_input_validation(is_valid: boolean, feedback_valid: string, feedback_invalid: string) {
        if (!item_current_form || !item_current_form._ks_info.show_validation) {
            return;
        }
        next_input_validation = {
            is_valid: is_valid,
            feedback_valid: feedback_valid,
            feedback_invalid: feedback_invalid,
        };
    }

    function consume_next_input_validation() {
        if (!next_input_validation) { return; }

        let el = recycle_set_current('DIV');
        set_class_name(el, next_input_validation.is_valid ? 'valid-feedback' : 'invalid-feedback');
        set_inner_text(el, next_input_validation.is_valid ?
            next_input_validation.feedback_valid : next_input_validation.feedback_invalid);
        next_input_validation = undefined;
    }

    function item_type_text_input(type: string) {
        switch (type) {
            case 'text':
                return Item_Type.input_text;
            case 'text_area':
                return Item_Type.input_text_area;
            case 'password':
                return Item_Type.input_password;
            case 'number':
                return Item_Type.input_number;
            case 'file':
                return Item_Type.input_file;
            case 'date':
                return Item_Type.input_date;
            default:
                console.error('unknown type', type);
                return Item_Type.input_text;
        }
    }

    function text_input(type: string, id_label: string, str, placeholder, proc_change): HTMLElement {
        let item_type = item_type_text_input(type);

        function set_on_input(el, proc) {
            let parent_stored = item_current_parent;
            let form_stored = item_current_form;
            let modal_stored = item_current_modal;
            let f = function () {
                push_state(el, parent_stored, form_stored, modal_stored);

                push_set_id(parent_stored._ks_info.this._ks_info.id);
                el._ks_input_value = item_type === Item_Type.input_number ? parseFloat(el.value) : el.value;
                proc.apply(parent_stored._ks_info.this, [item_type === Item_Type.input_file ? el.files : el._ks_input_value]);
                pop_id();

                pop_state();
            };
            if (is_ms_edge) { el._ks_info.onchange = f; }
            else { el._ks_info.oninput = f; }
        }

        // Local function does not properly capture here, so define as variable.
        // TODO: might not want to capture anyway for performance?
        let do_input = function () {
            let class_valid = !next_input_validation ? '' : (next_input_validation.is_valid ? ' is-valid' : ' is-invalid');
            let id = hash_str(id_label, id_chain[id_chain.length - 1]);
            let existing = item_existing(id, item_type);
            if (existing) {
                set_class_name(existing, 'form-control' + class_valid);
                if (existing._ks_input_value !== str) {
                    if (item_type !== Item_Type.input_number || !isNaN(str)) { existing.value = str; }
                    else if (item_type === Item_Type.input_number && str === undefined) { existing.value = ''; }
                    existing._ks_input_value = str;
                }
                if (existing._ks_info.placeholder !== placeholder) {
                    existing.placeholder = placeholder;
                    existing._ks_info.placeholder = placeholder;
                }
                append_set_current(existing);
                set_on_input(existing, proc_change);
                return existing;
            }

            let el: any = document.createElement(item_type === Item_Type.input_text_area ? 'TEXTAREA' : 'INPUT');
            item_info_add(el, item_type, id, label_extract(id_label));

            set_class_name(el, 'form-control' + class_valid);
            el.type = type;
            if (el._ks_input_value !== str) {
                if (item_type !== Item_Type.input_number || !isNaN(str)) { el.value = str; }
                else if (item_type === Item_Type.input_number && str === undefined) { el.value = ''; }
                el._ks_input_value = str;
            }
            el.placeholder = placeholder;
            el._ks_info.placeholder = placeholder;

            append_set_current(el, true);
            if (is_ms_edge) { el.onchange = item_onchange; }
            else { el.oninput = item_oninput; }
            set_on_input(el, proc_change);
            return el;
        };

        // set_next_item stuff might not behave as user would expect, because it applies to
        // the group element and not the input element if used inside a form.
        // User can however then manually create the groups to avoid this.
        if (item_current_form) {
            let is_row = item_current_parent._ks_info && item_current_parent._ks_info.type === Item_Type.row;
            if (is_row && item_current_parent.parentElement === item_current_form || item_current_parent === item_current_form) {
                set_next_item_this(item_current_parent);
                let el;
                group(id_label, is_row ? 'form-group col' : 'form-group', function () {
                    el = do_input();
                    consume_next_input_validation();
                });
                return el;
            } else {
                let el = do_input();
                consume_next_input_validation();
                return el;
            }
        }
        return do_input();
    }

    export function checkbox(label: string, is_checked: boolean, proc_input: (checked: boolean) => void): HTMLInputElement {
        return checkbox_radio_input(Item_Type.checkbox, label, is_checked, proc_input);
    }

    export function switch_button(label: string, is_checked: boolean, proc_input: (checked: boolean) => void): HTMLInputElement {
        return checkbox_radio_input(Item_Type.switch_button, label, is_checked, proc_input);
    }

    export function radio_button(label: string, is_checked: boolean, proc_input: Function): HTMLInputElement {
        return checkbox_radio_input(Item_Type.radio_button, label, is_checked, proc_input);
    }

    function do_check_radio_item(item_type: Item_Type, id_label: string, is_checked: boolean, proc_change): HTMLInputElement {
        set_next_item_this(this);
        return item_container(id_label, Item_Type.group, 'DIV', function () {
            let id = hash_str(id_label, id_chain[id_chain.length - 1]);
            let existing = item_existing(id, item_type);
            if (existing) {
                set_class_name(existing, 'custom-control-input');
                if (existing._ks_info.is_checked !== is_checked) {
                    existing._ks_info.is_checked = is_checked;
                    existing.checked = is_checked;
                }
                if (id_label !== existing._ks_info.label) { existing._ks_info.label = label_extract(id_label); }
                set_inner_text(existing._ks_info.el_label, existing._ks_info.label);

                append_set_current(existing);
                existing._ks_info.onchange = proc_change;
                existing._ks_info.form_stored = item_current_form;
                existing._ks_info.modal_stored = item_current_modal;
                return existing;
            }

            let label = label_extract(id_label);
            let el = <any>document.createElement('INPUT');
            let info = item_info_add(el, item_type, id, label);
            el.type = (item_type === Item_Type.radio_button && !is_microsoft) ? 'radio' : 'checkbox';
            el.checked = is_checked;
            set_class_name(el, 'custom-control-input');
            el.id = info.id;

            append_set_current(el, true);

            el._ks_info.onchange = proc_change;
            el._ks_info.form_stored = item_current_form;
            el._ks_info.modal_stored = item_current_modal;
            el.onchange = () => {
                el._ks_info.is_checked = el.checked;

                push_state(el, this, el._ks_info.form_stored, el._ks_info.modal_stored);

                push_set_id(this._ks_info.this._ks_info.id);
                el._ks_info.onchange.apply(this._ks_info.this, [el._ks_info.is_checked]);
                pop_id();

                pop_state();
            };

            let el_label = <HTMLLabelElement>document.createElement('LABEL');
            el_label.htmlFor = info.id;
            set_class_name(el_label, 'custom-control-label');
            set_inner_text(el_label, label);
            item_current_parent._ks_info.el_append.appendChild(el_label);

            info.el_label = el_label;
            info.is_checked = is_checked;

            if (item_type === Item_Type.radio_button) {
                // IE and Edge don't respond well to having this onclick event, simulate radio buttons as checkboxes
                if (!is_microsoft) {
                    el.onclick = el_label.onclick = function (ev) {
                        if (info.is_checked && el.checked) {
                            el._ks_info.onchange(undefined);
                            ev.preventDefault();
                            ev.stopPropagation();
                            return false;
                        }
                    };
                }
                el.onkeyup = function (ev) {
                    if (ev.code === 'Space' && info.is_checked) {
                        el._ks_info.onchange(undefined);
                        return false;
                    }
                };
            }
        }, function (el_wrapper) {
            let index_type = item_type - Item_Type.checkbox;
            let index_validation = !next_input_validation ? 0 : (next_input_validation.is_valid ? 1 : 2);
            set_class_name(el_wrapper, check_radio_class_names[index_validation * 3 + index_type]);
        });
    }

    let check_radio_class_names = [
        'custom-control custom-checkbox',
        'custom-control custom-radio',
        'custom-control custom-switch',
        'custom-control custom-checkbox is-valid',
        'custom-control custom-radio is-valid',
        'custom-control custom-switch is-valid',
        'custom-control custom-checkbox is-invalid',
        'custom-control custom-radio is-invalid',
        'custom-control custom-switch is-invalid'
    ];

    function checkbox_radio_input(type, id_label, is_checked, proc_change): HTMLInputElement {
        // set_next_item stuff might not behave as user would expect, because it applies to
        // the group element and not the input element if used inside a form.
        // User can however then manually create the groups to avoid this.
        if (item_current_form) {
            let is_row = item_current_parent._ks_info && item_current_parent._ks_info.type === Item_Type.row;
            if (is_row && item_current_parent.parentElement === item_current_form || item_current_parent === item_current_form) {
                set_next_item_this(item_current_parent);
                let el;
                group(id_label, is_row ? 'form-group col' : 'form-group', function () {
                    el = do_check_radio_item.call(this, type, id_label, is_checked, proc_change);
                    consume_next_input_validation();
                });
                return el;
            } else {
                let el = do_check_radio_item.call(item_current_parent, type, id_label, is_checked, proc_change);
                consume_next_input_validation();
                return el;
            }
        }
        return do_check_radio_item.call(item_current_parent, type, id_label, is_checked, proc_change);
    }

    // TODO: add multi event registration
    export function is_item_clicked(proc, cookie?) {
        if (!item_current) { return; }
        if (!item_current._ks_info) {
            item_info_add(item_current, Item_Type.fleeting, hash_fleeting(fleeting_id++, id_chain[id_chain.length - 1]));
            item_current.onclick = item_onclick;
            item_current.onmouseout = item_onmouseout;
            item_current.onmouseover = item_onmouseover;
        }

        let current_stored = item_current;
        let parent_stored = item_current_parent;
        let form_stored = item_current_form;
        let modal_stored = item_current_modal;
        let on_click = function (event) {
            push_state(current_stored, parent_stored, form_stored, modal_stored);

            push_set_id(current_stored._ks_info.id_chain);
            let result = proc.apply(parent_stored._ks_info.this, [cookie, event]);
            pop_id();

            pop_state();

            // Note: items that are removed during the proc will end up shifting things around in the DOM. 
            // This can lead to events propagating to parent elements that were not the original 
            // parent elements when the event occured.
            if (!document.body.contains(current_stored)) { event.stopPropagation(); }

            return result;
        };
        // Since onclick on <option> elements is not supported in chrome and mobile? browsers 
        // and it is unclear when it is/isn't supported, we simulate them by using the onchange event.
        if (item_current.tagName === 'OPTION') { item_current._ks_info.option_on_click = on_click; }
        else { item_current._ks_info.onclick = on_click; }
    }

    // TODO: add multi event registration
    export function is_item_hovered(): boolean {
        if (!item_current) { return false; }
        if (!item_current._ks_info) {
            let id = hash_fleeting(fleeting_id++, id_chain[id_chain.length - 1]);
            let info = item_info_add(item_current, Item_Type.fleeting, id);
            info.hovered = false;
            item_current.onclick = item_onclick;
            item_current.onmouseout = item_onmouseout;
            item_current.onmouseover = item_onmouseover;
        }

        let current_stored = item_current;
        let parent_stored = item_current_parent;
        item_current._ks_info.onmouseover = function (event) {
            event.stopPropagation();
            current_stored._ks_info.hovered = true;
            refresh(parent_stored, true);
        };
        item_current._ks_info.onmouseout = function (event) {
            event.stopPropagation();
            current_stored._ks_info.hovered = false;
            refresh(parent_stored, true);
        };
        return item_current._ks_info.hovered;
    }

    export function set_timeout(id: string, timeout_ms: number, proc: Function) {
        let hash = hash_str(id, id_chain[id_chain.length - 1]);
        let existing = item_existing(hash, Item_Type.set_timeout);
        if (existing) {
            let info = existing._ks_info;
            info.current_stored = item_current;
            info.parent_stored = item_current_parent;
            info.form_stored = item_current_form;
            info.modal_stored = item_current_modal;
            if (info.pass_id === pass_id) {
                let proc_existing = info.timeout_proc;
                info.timeout_proc = function () {
                    proc_existing.apply(info.parent_stored._ks_info.this);
                    proc.apply(info.parent_stored._ks_info.this);
                };
            } else {
                info.timeout_proc = proc
                info.pass_id = pass_id;
            }
            tree_item_buffer.push_child(tree_item, existing);
            return;
        }

        let el: any = document.createElement('DIV');
        el.className = 'd-none';
        let info = item_info_add(el, Item_Type.set_timeout, hash);
        info.pass_id = pass_id;
        info.timeout_proc = proc;
        info.current_stored = item_current;
        info.parent_stored = item_current_parent;
        info.form_stored = item_current_form;
        info.modal_stored = item_current_modal;

        window.setTimeout(function () {
            let existing = item_existing(hash, Item_Type.set_timeout);
            if (existing) {
                let info = existing._ks_info;
                push_state(info.current_stored, info.parent_stored, info.form_stored, info.modal_stored);
                push_set_id(info.parent_stored._ks_info.id);

                info.timeout_proc.apply(info.parent_stored._ks_info.this);

                pop_id();
                pop_state();
            }
        }, timeout_ms);

        item_append_child(item_current_parent, el);
        tree_item_buffer.push_child(tree_item, el);
    }

    export function set_interval(id: string, interval_ms: number, proc: Function, run_immediately = false) {
        let hash = hash_str(id, id_chain[id_chain.length - 1]);
        let existing = item_existing(hash, Item_Type.set_interval);
        if (existing) {
            let info = existing._ks_info;
            info.current_stored = item_current;
            info.parent_stored = item_current_parent;
            info.form_stored = item_current_form;
            info.modal_stored = item_current_modal;
            if (info.pass_id === pass_id) {
                let proc_existing = info.interval_proc;
                info.interval_proc = function () {
                    proc_existing.apply(info.parent_stored._ks_info.this);
                    proc.apply(info.parent_stored._ks_info.this);
                };
            } else {
                info.interval_proc = proc
                info.pass_id = pass_id;
            }
            tree_item_buffer.push_child(tree_item, existing);
            return;
        }

        let el: any = document.createElement('DIV');
        el.className = 'd-none';
        let info = item_info_add(el, Item_Type.set_interval, hash);
        info.pass_id = pass_id;
        info.interval_proc = proc;
        info.current_stored = item_current;
        info.parent_stored = item_current_parent;
        info.form_stored = item_current_form;
        info.modal_stored = item_current_modal;

        info.id_interval = window.setInterval(function () {
            let existing = item_existing(hash, Item_Type.set_interval);
            if (existing) {
                let info = existing._ks_info;
                push_state(info.current_stored, info.parent_stored, info.form_stored, info.modal_stored);
                push_set_id(info.parent_stored._ks_info.id);

                info.interval_proc.apply(info.parent_stored._ks_info.this);

                pop_id();
                pop_state();
            } else {
                window.clearInterval(info.id_interval);
            }
        }, interval_ms);

        item_append_child(item_current_parent, el);
        tree_item_buffer.push_child(tree_item, el);
        if (run_immediately) { proc.apply(info.parent_stored._ks_info.this); }
    }

    function item_info_add(el, type: Item_Type, id: number, label?: string, proc?) {
        el._ks_info = {
            id: id,
            id_chain: id_chain[id_chain.length - 1],
            label: label,
            type: type,
            el_append: el,
            proc: proc,
            pass_id: -1,
            this: el,
            children: [],
        };

        if (id in item_map) {
            console.error('Hash collision, item id already exists.', item_map[id]);
            return;
        }
        item_map[id] = el;
        return el._ks_info;
    }

    export function label_extract(id_label: string) {
        let index = id_label.indexOf('##');
        return index < 0 ? id_label : id_label.substring(0, index);
    }


    // cyrb53 hash
    function hash_str(str: string, seed: number): number {
        let len = str.length;
        let h1 = 0xdeadbeef ^ seed;
        let h2 = 0x41c6ce57 ^ seed;
        let c: number;
        for (let i = 0; i < len; ++i) {
            c = str.charCodeAt(i);
            // # = 35
            if (c === 35 && str.charCodeAt(i + 1) === 35 && str.charCodeAt(i + 2) === 35) {
                if (str.charCodeAt(i + 3) !== 35) {
                    h1 = 0xdeadbeef ^ seed;
                    h2 = 0x41c6ce57 ^ seed;
                } else { // #### is global id
                    h1 = 0xdeadbeef ^ 0;
                    h2 = 0x41c6ce57 ^ 0;
                    i += 3;
                }
            }

            h1 = Math.imul(h1 ^ c, 2654435761);
            h2 = Math.imul(h2 ^ c, 1597334677);
        }
        return 4294967296 * (2097151 & h2) + (h1 >>> 0);
    }

    function hash_int(val: number, seed: number): number {
        let h1 = 0xdeadbeef ^ seed;
        let h2 = 0x41c6ce57 ^ seed;
        h1 = Math.imul(h1 ^ val, 2654435761);
        h2 = Math.imul(h2 ^ val, 1597334677);
        return 4294967296 * (2097151 & h2) + (h1 >>> 0);
    }

    function hash_fleeting(val: number, seed: number) {
        // # = 35
        // Unrolled version of '####' + val without seed reset
        let h1 = 0xdeadbeef ^ seed;
        let h2 = 0x41c6ce57 ^ seed;

        h1 = Math.imul(h1 ^ 35, 2654435761);
        h2 = Math.imul(h2 ^ 35, 1597334677);
        h1 = Math.imul(h1 ^ 35, 2654435761);
        h2 = Math.imul(h2 ^ 35, 1597334677);
        h1 = Math.imul(h1 ^ 35, 2654435761);
        h2 = Math.imul(h2 ^ 35, 1597334677);
        h1 = Math.imul(h1 ^ 35, 2654435761);
        h2 = Math.imul(h2 ^ 35, 1597334677);

        h1 = Math.imul(h1 ^ val, 2654435761);
        h2 = Math.imul(h2 ^ val, 1597334677);
        return 4294967296 * (2097151 & h2) + (h1 >>> 0);
    }

    export function push_id(id: string) {
        id_chain.push(hash_str(id, id_chain[id_chain.length - 1]));
    }

    function push_set_id(id: number) {
        id_chain.push(id);
    }

    export function pop_id() {
        id_chain.pop();
    }

    function item_existing(id: number, type: Item_Type) {
        let item = item_map[id];
        if (item && item._ks_info.type !== type) {
            console.error('Item id exists, but types do not match. Hash collision?');
        }
        return item;
    }

    // Finds the current modal by checking up the parent element chain, returns undefined if not found.
    function find_current_modal(item_start_from) {
        let item = item_start_from;
        while (item) {
            if (item._ks_info && item._ks_info.type === Item_Type.modal) {
                return item;
            }
            item = item.parentElement;
        }
        return undefined;
    }

    // Finds the current form by checking up the parent element chain, returns undefined if not found.
    function find_current_form(item_start_from) {
        let item = item_start_from;
        while (item) {
            if (item._ks_info && item._ks_info.type === Item_Type.form) {
                return item;
            }
            item = item.parentElement;
        }
        return undefined;
    }

    function push_state(current, parent, form_arg, modal_arg) {
        state_stack.push(item_current, item_current_parent, item_current_form, item_current_modal);
        item_current = current;
        item_current_parent = parent;
        item_current_form = form_arg;
        item_current_modal = modal_arg;
    }

    function pop_state() {
        let state = state_stack.pop();
        item_current_parent = state.parent;
        item_current_form = state.form;
        item_current_modal = state.modal;
    }

    let navigate_after_refresh = false;
    let refresh_may_recurse = false;
    export function refresh(item?, allow_recursive_refresh?: boolean) {
        if (!item) { item = container; }
        if (is_refresh && !refresh_may_recurse) { return; }

        let prev_refresh_may_recurse = refresh_may_recurse;
        refresh_may_recurse = !!allow_recursive_refresh;

        let chain_length = id_chain.length;
        let is_recursive_refresh = is_refresh;
        is_refresh = true;
        ++pass_id;

        let info = item._ks_info;
        if (info && info.proc) {
            let el_restore_focus = <HTMLElement>document.activeElement;

            push_state(item, item, find_current_form(item), find_current_modal(item));

            let prev_tree_item;
            let prev_tree_item_map;
            if (is_recursive_refresh) {
                prev_tree_item = tree_item;
                prev_tree_item_map = tree_item_map;
            }

            let i_buffer = tree_item_buffer.length;
            tree_item = tree_item_buffer.push(item);
            tree_item_map = {};
            tree_item_map[item._ks_info.id] = tree_item;

            push_set_id(info.id);
            info.proc.apply(item._ks_info.this);
            pop_id();

            for (let i = i_buffer; i < tree_item_buffer.length; ++i) {
                let node = tree_item_buffer.items[i];
                if (node.parent) {
                    let parent_children = item_children(node.parent.el);
                    let c = parent_children[node.i_child];

                    while (c && !c._ks_info && (!c._ks_tag_name || c !== node.el)) {
                        item_remove_child(node.parent.el, node.i_child);
                        c = parent_children[node.i_child];
                    }

                    if (c !== node.el) { item_insert_child(node.parent.el, node.el, node.i_child); }
                }
            }

            for (let i = i_buffer; i < tree_item_buffer.length; ++i) {
                let node = tree_item_buffer.items[i];
                if (node.el._ks_info) {
                    let children = item_children(node.el);
                    for (let i_child = children.length - 1; i_child >= node.child_count; --i_child) {
                        let el_child = children[i_child];
                        remove_item(el_child);
                        item_remove_child(node.el, i_child);
                    }
                }
            }

            tree_item_buffer.clear(i_buffer);

            if (is_recursive_refresh) {
                tree_item = prev_tree_item;
                tree_item_map = prev_tree_item_map;
            }

            if (el_restore_focus && el_restore_focus !== document.activeElement && document.body.contains(el_restore_focus)) {
                el_restore_focus.focus();
            }

            pop_state();
        }

        is_refresh = is_recursive_refresh;
        refresh_may_recurse = prev_refresh_may_recurse;

        if (chain_length < id_chain.length) {
            console.error('Missing ' + (id_chain.length - chain_length) + ' pop_id() calls.');
        }
        if (id_chain.length < chain_length) {
            console.error('No matching push_id() for ' + (chain_length - id_chain.length) + ' pop_id() calls.');
        }

        if (navigate_after_refresh && !is_refresh) {
            navigate_after_refresh = false;
            refresh(container);
        }
    }

    function remove_item(el) {
        if (!el._ks_info) { return; }

        delete item_map[el._ks_info.id];

        let children = item_children(el);
        for (let i = 0; i < children.length; ++i) { remove_item(children[i]); }
    }

    // returns el
    function temp_switch_parent_apply(el, proc, is_new = false, state_form?, state_modal?) {
        if (is_new) {
            item_append_child(item_current_parent, el);
            el.onclick = item_onclick;
            el.onmouseout = item_onmouseout;
            el.onmouseover = item_onmouseover;
        }
        el._ks_info.onclick = undefined;
        el._ks_info.onmouseout = undefined;
        el._ks_info.onmouseover = undefined;

        push_state(el, el, state_form || item_current_form, state_modal || item_current_modal);

        if (next_item_this) {
            el._ks_info.this = next_item_this;
            next_item_this = undefined;
        }
        if (el._ks_info.pass_id === pass_id) {
            let proc_existing = el._ks_info.proc;
            el._ks_info.proc = function () {
                proc_existing.apply(el._ks_info.this);
                proc.apply(el._ks_info.this);
            };
        } else {
            el._ks_info.proc = proc;
            el._ks_info.pass_id = pass_id;
        }

        let prev_tree_item = tree_item;
        console.assert(tree_item !== null);
        tree_item = tree_item_map[el._ks_info.id];
        if (!tree_item) {
            tree_item = tree_item_buffer.push_child(prev_tree_item, el);
            tree_item_map[el._ks_info.id] = tree_item;
        }

        proc.apply(el._ks_info.this);
        pop_id();

        tree_item = prev_tree_item;

        item_current = el;
        pop_state();
        return el;
    }

    // returns el
    function append_set_current(el, is_new = false) {
        if (is_new) {
            item_append_child(item_current_parent, el);
            el.onclick = item_onclick;
            el.onmouseout = item_onmouseout;
            el.onmouseover = item_onmouseover;
        }
        el._ks_pass_id = pass_id;

        if (el._ks_info) {
            el._ks_info.onclick = undefined;
            el._ks_info.onmouseout = undefined;
            el._ks_info.onmouseover = undefined;
        }
        item_current = el;

        tree_item_buffer.push_child(tree_item, el);

        return el;
    }

    function recycle_set_current(tag_name) {
        let children = item_children(item_current_parent);
        if (children.length) {
            let el = children[tree_item.child_count];
            // Make sure we don't recycle newly added elements in this pass by checking for pass_id
            if (el && el._ks_pass_id !== pass_id && el._ks_tag_name === tag_name) {
                item_current = el;
                if (el._ks_info) {
                    delete item_map[el._ks_info.id];
                    el._ks_info.onclick = undefined;
                    el._ks_info.onmouseout = undefined;
                    el._ks_info.onmouseover = undefined;
                }
                el._ks_pass_id = pass_id;
                tree_item_buffer.push_child(tree_item, el);
                return el;
            }
        }

        let el = document.createElement(tag_name);
        el._ks_tag_name = tag_name;
        return append_set_current(el, true);
    }


    function set_next_item_this(this_arg) {
        next_item_this = this_arg;
    }

    export function set_next_item_class_name(class_name: string) {
        next_item_class_name = class_name;
    }

    // Prevent layout trashing with these functions
    function set_class_name(el, class_name?) {
        // Only set the class name the first time we come across this item within this pass
        if (el._ks_info && el._ks_info.pass_id === pass_id) {
            next_item_class_name = undefined;
            return;
        }

        // TODO: we can probably avoid some string allocations by deferring string concat and compare first
        // maybe store next_item_class_name in item for fast compare?
        if (next_item_class_name) {
            class_name = class_name ? class_name + ' ' + next_item_class_name : next_item_class_name;
        }
        if (el._ks_class_name !== class_name) {
            el._ks_class_name = class_name;
            el.className = class_name;
        }
        next_item_class_name = undefined;
    }

    function set_inner_text(el, str) {
        // Even though textContent is not supposed to thrash layout, we see a 
        // performance upgrade by only settings it when necessary
        if (el._ks_inner_text !== str) {
            el.textContent = str;
            el._ks_inner_text = str;
        }
    }

    function set_input_value(el, value) {
        if (el._ks_input_value !== value) {
            el.value = value;
            el._ks_input_value = value;
        }
    }

    function item_children(el) {
        return el._ks_info.children;
    }

    function item_append_child(el, child) {
        if (child._ks_el_parent) {
            let index = child._ks_el_parent._ks_info.children.indexOf(child);
            if (index >= 0) { item_remove_child(child._ks_el_parent, index); }
        }
        child._ks_el_parent = el;
        el._ks_info.el_append.appendChild(child);
        el._ks_info.children.push(child);
    }

    function item_insert_child(el, child, index) {
        if (child._ks_el_parent) {
            let i = child._ks_el_parent._ks_info.children.indexOf(child);
            if (i >= 0) { item_remove_child(child._ks_el_parent, i); }
        }
        child._ks_el_parent = el;
        el._ks_info.el_append.insertBefore(child, el._ks_info.children[index]);
        el._ks_info.children.splice(index, 0, child);
    }

    function item_remove_child(el, index: number) {
        el._ks_el_parent = undefined;
        el._ks_info.el_append.removeChild(el._ks_info.children[index]);
        el._ks_info.children.splice(index, 1);
    }

    function item_onclick(ev) {
        let el = ev.currentTarget;
        if (el._ks_info && el._ks_info.onclick) { return el._ks_info.onclick(ev); }
    }

    function item_onchange(ev) {
        let el = ev.currentTarget;
        if (el._ks_info && el._ks_info.onchange) { el._ks_info.onchange(ev); }
    }

    function item_oninput(ev) {
        let el = ev.currentTarget;
        if (el._ks_info && el._ks_info.oninput) { el._ks_info.oninput(ev); }
    }

    function item_onmouseout(ev) {
        let el = ev.currentTarget;
        if (el._ks_info && el._ks_info.onmouseout) { el._ks_info.onmouseout(ev); }
    }

    function item_onmouseover(ev) {
        let el = ev.currentTarget;
        if (el._ks_info && el._ks_info.onmouseover) { el._ks_info.onmouseover(ev); }
    }

    export function no_op() { }

    export function local_persist<T extends object>(id: string, initial_value?: T): T {
        let hash = hash_str(id, id_chain[id_chain.length - 1]);
        let value = local_persists[hash];
        if (value) { return value; }
        local_persists[hash] = initial_value;
        return initial_value;
    }

    // Utility functions for custom extensions
    export function get_current_item() {
        return item_current;
    }

    export function get_current_parent() {
        return item_current_parent;
    }
}