class ConfirmModal {
    id = '####confirm_modal';
    message = '';
    el: HTMLElement;
    proc;

    confirm(message: string, proc: (confirmed: boolean) => void) {
        this.message = message;
        this.proc = proc;
        ks.refresh(this.el);
        ks.open_popup(this.id);
    }

    run() {
        let modal = this;
        modal.el = ks.popup_modal(modal.id, function () {
            ks.set_next_item_class_name('bg-warning');
            ks.modal_header(function () {
                ks.h5('Warning', 'modal-title text-light');
            });
            ks.modal_body(function () {
                ks.text(modal.message);
            });
            ks.modal_footer(function () {
                ks.button('Yes', function () {
                    modal.proc(true);
                    ks.close_current_popup();
                });
                ks.button('Close', function () {
                    modal.proc(false);
                    ks.close_current_popup();
                });
            });
        }, false, false, true);
    }
}
class ContextualModal {
    id = '####contextual_modal';
    header: string;
    css_class: 'warning' | 'success';
    icon_class: 'fa-exclamation-triangle text-warning' | 'fa-check-circle-o text-success';
    str_or_proc: string | Function = '';
    el: HTMLElement;

    showSuccess(str_or_proc: string | Function, size?: ks.Modal_Size) {
        this.header = 'Success';
        this.str_or_proc = str_or_proc;
        this.css_class = 'success';
        this.icon_class = 'fa-check-circle-o text-success';

        ks.refresh(this.el);
        this.update_size(size);
        ks.open_popup(this.id);
    }
    showWarning(str_or_proc: string | Function, size?: ks.Modal_Size) {
        this.header = 'Warning';
        this.str_or_proc = str_or_proc;
        this.css_class = 'warning';
        this.icon_class = 'fa-exclamation-triangle text-warning';

        ks.refresh(this.el);
        this.update_size(size);
        ks.open_popup(this.id);
    }

    run() {
        let modal = this;
        modal.el = ks.popup_modal(modal.id, function () {
            ks.set_next_item_class_name('text-center px-4 py-5');
            ks.modal_body(function () {
                ks.icon(`fa ${modal.icon_class} mb-2`).style.fontSize = '2.25rem';
                ks.h5(modal.header, 'mb-3');

                if (typeof modal.str_or_proc === 'function') {
                    modal.str_or_proc();
                } else { ks.text(modal.str_or_proc); }

                ks.set_next_item_class_name('mt-4');
                ks.button('Close', function () {
                    ks.close_current_popup();
                }, modal.css_class);
            });
        }, true, false, true);
    }

    private update_size(size: ks.Modal_Size) {
        let size_class: string;
        switch (size) {
            case ks.Modal_Size.small:
                size_class = 'modal-dialog modal-sm';
                break;
            case ks.Modal_Size.large:
                size_class = 'modal-dialog modal-lg';
                break;
            case ks.Modal_Size.extra_large:
                size_class = 'modal-dialog modal-xl';
                break;
            default:
                size_class = 'modal-dialog';
                break;
        }
        (<any>this.el)._ks_info.el_dlg.className = size_class;
    }
}