class ConfirmModal {
    id = '####confirm_modal';
    header: string;
    message: string;
    el: HTMLElement;
    proc;

    show(header: string, message: string, proc: (confirmed: boolean) => void) {
        this.header = header;
        this.message = message;
        this.proc = proc;
        ks.refresh(this.el);
        ks.open_popup(this.id);
    }

    run() {
        let modal = this;
        modal.el = ks.popup_modal(modal.id, function () {
            ks.set_next_item_class_name('text-center px-4 py-5');
            ks.modal_body(function () {
                ks.icon('fa fa-question-circle-o text-info mb-2').style.fontSize = '2.5rem';
                if (modal.header) { ks.h5(modal.header, 'mb-3'); }

                if (modal.message) { ks.text(modal.message); }

                ks.set_next_item_class_name('mt-4');
                ks.group('btns', '', function () {
                    ks.button('Cancel', function () {
                        modal.proc(false);
                        ks.close_current_popup();
                    }, 'outline-secondary mr-2');
                    ks.button('Yes', function () {
                        modal.proc(true);
                        ks.close_current_popup();
                    }, 'info');
                });
            });
        }, true, false, true);
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

class NoteModal {
    id = '####note_modal';
    note = '';
    proc_save: Function;
    el: HTMLElement;

    show(note: string, proc_save?: (note: string) => void) {
        this.note = note || '';
        this.proc_save = proc_save;
        ks.refresh(this.el);
        ks.open_popup(this.id);
    }

    run() {
        let modal = this;
        modal.el = ks.popup_modal(modal.id, function () {
            ks.modal_body(function () {
                if (modal.proc_save) {
                    ks.input_text_area('note', modal.note,
                        'Add a note to this device. This note is visible to the device owner.', function (str) {
                            modal.note = str;
                        });

                    ks.group('close', 'text-center mt-4', function () {
                        ks.button('Close', function () {
                            ks.close_current_popup();
                        }, 'light mr-2');
                        ks.button('Save', function () {
                            ks.close_current_popup();
                            modal.proc_save(modal.note);
                        }, 'warning');
                    });
                } else {
                    ks.text(modal.note);
                }
            });
        }, true, true, true);

        let modalContent = <HTMLElement>modal.el.firstChild.firstChild;
        modalContent.style.color = '#856404';
        modalContent.style.borderColor = '#ffeeba';
        modalContent.style.backgroundColor = '#fff3cd';
    }
}