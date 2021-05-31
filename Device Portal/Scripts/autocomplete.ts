﻿/*!*****************************************************************************************************

    MIT License
    
    Copyright (c) 2019 Honatas
    
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:
    
    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.
    
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.

*******************************************************************************************************/

interface AutocompleteItem {
    value: string,
    label: string,
}

interface AutocompleteOptions {
    dropdownOptions?: Bootstrap.DropdownOption,
    highlightClass?: string,
    highlightTyped?: boolean,
    label?: string,
    maximumItems?: number,
    onSelectItem?: (item: AutocompleteItem, element: HTMLElement) => void,
    source?: object,
    treshold?: number,
    value?: string,
}

interface JQuery {
    autocomplete(options: AutocompleteOptions): JQuery<HTMLElement>;
}

(function ($) {

    let defaults: AutocompleteOptions = {
        treshold: 4,
        maximumItems: 5,
        highlightTyped: true,
        highlightClass: 'text-primary',
    };

    function createItem(lookup: string, item: AutocompleteItem, opts: AutocompleteOptions): string {
        let label: string;
        if (opts.highlightTyped) {
            const idx = item.label.toLowerCase().indexOf(lookup.toLowerCase());
            label = item.label.substring(0, idx)
                + '<span class="' + opts.highlightClass + '">' + item.label.substring(idx, idx + lookup.length) + '</span>'
                + item.label.substring(idx + lookup.length, item.label.length);
        } else {
            label = item.label;
        }
        return '<button type="button" class="dropdown-item" data-value="' + item.value + '">' + label + '</button>';
    }

    function createItems(field: JQuery<HTMLElement>, opts: AutocompleteOptions) {
        const lookup = field.val() as string;
        if (lookup.length < opts.treshold) {
            field.dropdown('hide');
            return 0;
        }

        const items = field.nextAll('.dropdown-menu');
        items.html('');

        let count = 0;
        const keys = Object.keys(opts.source);
        for (let i = 0; i < keys.length; i++) {
            const key = keys[i];
            const object = opts.source[key];
            const item = {
                label: opts.label ? object[opts.label] : key,
                value: opts.value ? object[opts.value] : object,
            };
            if (item.label.toLowerCase().indexOf(lookup.toLowerCase()) >= 0) {
                items.append(createItem(lookup, item, opts));
                if (++count >= opts.maximumItems) {
                    break;
                }
            }
        }

        // option action
        field.nextAll('.dropdown-menu').find('.dropdown-item').click(function () {
            field.val($(this).text());
            if (opts.onSelectItem) {
                opts.onSelectItem({
                    value: $(this).data('value'),
                    label: $(this).text(),
                }, field[0]);
            }
        });

        return items.children().length;
    }

    $.fn.autocomplete = function (options) {
        // merge options with default
        let opts: AutocompleteOptions = {};
        $.extend(opts, defaults, options);

        let _field = $(this);            

        // attach dropdown
        if (!_field.parent().hasClass('dropdown')) {
            _field.parent().addClass('dropdown');
            _field.attr('data-toggle', 'dropdown');
            _field.addClass('dropdown-toggle');
            _field.after('<div class="dropdown-menu"></div>');
            _field.dropdown(opts.dropdownOptions);
        }

        _field.off('click.autocomplete').on('click.autocomplete', function (e) {
            if (createItems(_field, opts) == 0) {
                // prevent show empty
                e.stopPropagation();
                _field.dropdown('hide');
            };
        });

        // show options
        _field.off('keyup.autocomplete').on('keyup.autocomplete', function () {
            if (createItems(_field, opts) > 0) {
                _field.dropdown('show');
            } else {
                // sets up positioning
                _field.click();
            }
        });

        return this;
    };
}(jQuery));