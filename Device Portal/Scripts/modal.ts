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
    el: HTMLElement;

    show(note: string) {
        this.note = note || '';
        ks.refresh(this.el);
        ks.open_popup(this.id);
    }

    run() {
        let modal = this;
        modal.el = ks.popup_modal(modal.id, function () {
            ks.modal_body(function () {
                ks.text(modal.note);
            });
        }, true, true, true);

        let modalContent = <HTMLElement>modal.el.firstChild.firstChild;
        modalContent.style.color = '#856404';
        modalContent.style.borderColor = '#ffeeba';
        modalContent.style.backgroundColor = '#fff3cd';
    }
}

namespace DP {
    export class DeviceModal {
        id = '####device_modal';
        note = '';
        shared = false;
        device: Device;
        el: HTMLElement;

        show(device: Device) {
            this.note = device.notes || '';
            this.shared = device.shared;
            this.device = device;
            ks.refresh(this.el);
            ks.open_popup(this.id);
        }

        run() {
            let modal = this;

            ks.set_next_modal_size(ks.Modal_Size.large);
            modal.el = ks.popup_modal(modal.id, function () {
                if (!modal.device) { return; }

                let d = <DeviceUser>modal.device;
                let scale = iconScale(d.type);

                ks.group('bg', 'position-absolute w-100 h-100 overflow-hidden', function () {
                    let icon = ks.icon(deviceIcon(d.type) + ' position-absolute text-secondary');
                    icon.style.fontSize = scale * 30 + 'rem';
                    icon.style.transform = 'rotate(-15deg)';
                    icon.style.top = iconTop(d.type);
                    icon.style.right = '50px';
                    icon.style.opacity = '0.03';
                }).style.pointerEvents = 'none';

                ks.set_next_item_class_name('text-center px-4 py-5');
                ks.modal_body(function () {
                    ks.icon(deviceIcon(d.type) + ' text-primary mb-2').style.fontSize = scale * 2 + 'rem';
                    ks.h5(d.name, 'mb-3');

                    ks.row('row', function () {
                        ks.column('left', 6, function () {
                            detail('Type', deviceTypes[d.type]);
                            detail('User', d.user);

                            if (!(d.category & (DeviceCategory.ManagedSpecial | DeviceCategory.ManagedStandard))) {
                                ks.text('Status', 'font-weight-bold');
                                ks.text(statusNames[d.status], 'mb-3 badge badge-' + statusColors[d.status]);
                            }

                            detail('Category', deviceCategories[d.category]);
                            detail('Cost centre', d.costCentre);
                            detail('Building', d.itracsBuilding);
                            detail('Room', d.itracsRoom);
                            detail('Outlet', d.itracsOutlet);
                        });

                        ks.column('right', 6, function () {
                            detail('Device ID', d.deviceId);

                            ks.text('Shared device', 'font-weight-bold');
                            ks.set_next_item_class_name('mb-3');
                            ks.switch_button('Multiple users', modal.shared, function (checked) {
                                modal.shared = checked;
                                ks.refresh(modal.el);
                            });

                            detail('Serial number', d.serialNumber);
                            detail('MAC address', d.macadres);
                            detail('OS', osNames[d.os_type]);
                            detail('OS version', d.os_version);
                            detail('Purchase date', d.purchaseDate);
                            detail('Last seen', d.lastSeenDate);
                        });
                    });

                    ks.group('note_btns', 'px-5', function () {
                        ks.text('Note', 'font-weight-bold py-1');
                        ks.input_text_area('note', modal.note,
                            'Add a note to this device. This note is visible to the device owner.', function (str) {
                                modal.note = str;
                                ks.refresh(this);
                        });

                        ks.group('btns', 'mt-3', function () {
                            ks.button('Close', function () {
                                modal.note = null;
                                modal.device = null;
                                ks.close_current_popup();
                            }, 'outline-secondary mr-2');

                            let disabled = modal.note === (d.notes || '') && modal.shared === d.shared;
                            ks.button('Save', function () {
                                let noteOld = d.notes;
                                let sharedOld = d.shared;
                                d.notes = modal.note;
                                d.shared = modal.shared;

                                // Device might be the derived DeviceUser class, we don't want those properties set
                                let entity: any = {};
                                for (let key in d) { entity[key] = d[key]; }
                                entity.user = null;
                                entity.userName = null;
                                entity.email = null;

                                ks.close_current_popup();

                                PUT_JSON(API.Devices(d.id), entity).then(() => {
                                    contextModal.showSuccess('Changes successfully saved.');
                                    ks.refresh();
                                }, fail => {
                                    d.notes = noteOld;
                                    d.shared = sharedOld;
                                    contextModal.showWarning(fail.responseText);
                                    ks.refresh();
                                });
                            }, 'primary' + (disabled ? ' disabled' : '')).disabled = disabled;
                        });
                    });
                });
            }, true, true, true);
        }
    }

    function iconScale(type: DeviceType): number {
        switch (type) {
            case DeviceType.Mobile:
                return 1.3;
            case DeviceType.Tablet:
                return 1.2;
            case DeviceType.Laptop:
                return 1.1;
            default:
                return 1;
        }
    }

    function iconTop(type: DeviceType): string {
        switch (type) {
            case DeviceType.Mobile:
                return '0px';
            case DeviceType.Tablet:
                return '65px';
            case DeviceType.Laptop:
                return '45px';
            default:
                return '130px';
        }
    }

    function detail(title: string, text: string) {
        if (!text) { return; }
        ks.text(title, 'font-weight-bold');
        ks.text(text, 'mb-3');
    }
}