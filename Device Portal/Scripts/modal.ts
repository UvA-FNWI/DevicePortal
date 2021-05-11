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
        showTimeline = false;
        device: Device;
        display: Device;
        history: DeviceHistory[];
        el: HTMLElement;

        show(device: Device) {
            this.note = device.notes || '';
            this.shared = device.shared;
            this.device = device;
            this.display = device;
            this.history = null;
            this.showTimeline = false;
            ks.refresh(this.el);
            ks.open_popup(this.id);
        }

        run() {
            let modal = this;

            ks.set_next_modal_size(ks.Modal_Size.large);
            modal.el = ks.popup_modal(modal.id, function () {
                if (!modal.display) { return; }

                let d = <Device>modal.display;
                let is_current = modal.device === modal.display;
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

                    if (!is_current) {
                        ks.set_next_item_class_name('mx-5');
                        ks.alert_box('alert', 'warning', true, function () {
                            ks.text('Attention', 'font-weight-bold');
                            let date = (<DeviceHistory>modal.display).dateHistory;
                            ks.text('You are viewing an old record edited on ' + date + ' by ' + d.userEditName + '.');
                            ks.anchor('Return to most recent', '');
                            ks.is_item_clicked(function (_, ev) {
                                ev.preventDefault();
                                modal.display = modal.device;
                                modal.note = modal.device.notes;
                                modal.shared = modal.device.shared;
                                ks.refresh(modal.el);
                            });
                        });
                    }

                    ks.row('row', function () {
                        ks.column('left', 6, function () {
                            detail('Type', deviceTypes[d.type]);
                            detail('User', d.user?.name);

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
                            ks.set_next_item_class_name('mb-3' + (is_current ? '' : ' disabled'));
                            ks.switch_button('Multiple users', modal.shared, function (checked) {
                                modal.shared = checked;
                                ks.refresh(modal.el);
                            }).disabled = !is_current;

                            detail('Serial number', d.serialNumber);
                            detail('MAC address', d.macadres);
                            detail('OS', osNames[d.os_type]);
                            detail('OS version', d.os_version);
                            detail('Purchase date', d.purchaseDate);
                            detail('Last seen', d.lastSeenDate);
                            detail('Edited by', d.userEditName);
                        });
                    });


                    ks.group('note', 'px-5', function () {
                        ks.text('Note', 'font-weight-bold py-1');
                        if (!is_current) { ks.set_next_item_class_name('disabled'); }
                        ks.input_text_area('note', modal.note,
                            'Add a note to this device. This note is visible to the device owner.', function (str) {
                                modal.note = str;
                                ks.refresh(modal.el);
                        }).disabled = !is_current;
                    });

                    ks.set_next_item_class_name('d-block mt-2');
                    ks.anchor(modal.showTimeline ? 'Hide history###view_history' : 'View history###view_history', '');
                    ks.is_item_clicked(function (_, ev) {
                        ev.preventDefault();

                        if (modal.showTimeline) {
                            modal.showTimeline = false;
                            ks.refresh(modal.el);
                        } else {
                            GET_ONCE('history', API.Devices(modal.device.id + '/History')).done((history: DeviceHistory[]) => {
                                for (let i = 0; i < history.length; ++i) {
                                    let h = history[i];
                                    let index = h.dateHistory.indexOf('T');
                                    if (index > 0) { h.dateHistory = h.dateHistory.substring(0, index); }

                                    Device.formatPurchaseDate(h);
                                    Device.formatLastSeenDate(h);
                                }
                                modal.showTimeline = true;
                                modal.history = history;
                                ks.refresh();
                            });
                        }
                    });

                    if (modal.showTimeline) { modal.timeline(); }

                    ks.group('btns', 'mt-3', function () {
                        ks.button('Close', function () {
                            modal.showTimeline = false;
                            modal.note = null;
                            modal.device = null;
                            modal.display = null;
                            ks.close_current_popup();
                        }, 'outline-secondary mr-2');

                        let disabled = modal.device !== modal.display || modal.note === (d.notes || '') && modal.shared === d.shared;
                        ks.button('Save', function () {
                            let noteOld = d.notes;
                            let sharedOld = d.shared;
                            d.notes = modal.note;
                            d.shared = modal.shared;

                            let entity: any = {};
                            for (let key in d) { entity[key] = d[key]; }
                            // These dates are modified for display purposes, but could be unparsable by back-end
                            entity.lastSeenDate = null;
                            entity.purchaseDate = null;

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
            }, true, true, true);
        }

        timeline() {
            let modal = this;
            ks.group('timeline', 'timeline d-inline-block text-left px-5 mt-3 mb-1', function () {
                if (!modal.history) { return; }

                if (!modal.history.length) {
                    ks.text('No further history', 'm-n3');
                }

                if (modal.history.length) {
                    let cls = modal.display !== modal.device ? 'timeline-item cursor-pointer' : 'timeline-item';
                    ks.group('##most recent', cls, function () {
                        ks.icon('fa fa-clock-o timeline-icon bg-primary text-light');
                        ks.group('content', 'timeline-item-content', function () {
                            ks.text('Most recent', modal.device === modal.display ? 'font-weight-bold text-primary' : 'font-weight-bold');
                            modal.timeline_diff(modal.history[0], modal.device);
                            ks.text('Edited by ' + (modal.device.userEditName || '<unknown>'), 'text-muted').style.fontSize = '0.8rem';
                        });
                    });
                    ks.is_item_clicked(function () {
                        modal.display = modal.device;
                        modal.note = modal.device.notes;
                        modal.shared = modal.device.shared;
                        ks.refresh(modal.el);
                    });
                }

                for (let i = 0; i < modal.history.length; ++i) {
                    let h = modal.history[i];
                    let p = modal.history[i + 1];

                    let cls = modal.display !== h ? 'timeline-item cursor-pointer' : 'timeline-item';
                    ks.group('' + h.id, cls, function () {
                        ks.icon('fa fa-clock-o timeline-icon bg-primary text-light');
                        ks.group('content', 'timeline-item-content', function () {
                            ks.text(h.dateHistory, h === modal.display ? 'font-weight-bold text-primary' : 'font-weight-bold');
                            modal.timeline_diff(p, h);
                            let label = (i + 1) === modal.history.length ? 'Added by ' : 'Edited by ';
                            ks.text(label + (h.userEditName || '<unknown>'), 'text-muted').style.fontSize = '0.8rem';
                        });
                    });
                    ks.is_item_clicked(function () {
                        modal.display = h;
                        modal.note = h.notes;
                        modal.shared = h.shared;
                        ks.refresh(modal.el);
                    });
                }
            });
        }

        timeline_diff(previous: Device, current: Device) {
            if (!previous) { return; }
            let p = previous;
            let c = current;

            diff('Name: ', p.name, c.name);
            diff('Type: ', deviceTypes[p.type], deviceTypes[c.type]);
            diff('User:', p.user?.name, c.user?.name);

            if (p.status !== c.status) {
                ks.text('Status: ', 'd-inline');
                ks.text(statusNames[p.status], 'd-inline-block badge badge-' + statusColors[p.status]);
                ks.text(' → ', 'd-inline');
                ks.text(statusNames[c.status], 'badge badge-' + statusColors[c.status]);
            }

            diff('Category: ', deviceCategories[p.category], deviceCategories[c.category]);
            diff('Cost centre: ', p.costCentre, c.costCentre);
            diff('Building: ', p.itracsBuilding, c.itracsBuilding);
            diff('Room: ', p.itracsRoom, c.itracsRoom);
            diff('Outlet: ', p.itracsOutlet, c.itracsOutlet);
            diff('Device ID: ', p.deviceId, c.deviceId);

            if (p.shared !== c.shared) {
                ks.text('Shared device: ', 'd-inline');
                ks.set_next_item_class_name('disabled d-inline');
                ks.switch_button('##multi users from', p.shared, ks.no_op).disabled = true;
                ks.text('→ ', 'd-inline');
                ks.set_next_item_class_name('disabled d-inline');
                ks.switch_button('##multi users to', c.shared, ks.no_op).disabled = true;
            }

            diff('Serial number: ', p.serialNumber, c.serialNumber);
            diff('MAC address: ', p.macadres, c.macadres);
            diff('OS: ', osNames[p.os_type], osNames[c.os_type]);
            diff('OS version: ', p.os_version, c.os_version);
            diff('Purchase date: ', p.purchaseDate, c.purchaseDate);
            diff('Last seen: ', p.lastSeenDate, c.lastSeenDate);
            if (p.notes !== c.notes) {
                if (c.notes) {
                    ks.text('Note: ', 'd-inline');
                    ks.text(c.notes, 'font-italic d-inline clearfix');
                } else {
                    ks.text('Note removed');
                }
            }
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

    function diff(label: string, from: string, to: string) {
        if (from !== to) {
            ks.text(label, 'd-inline');
            ks.text(from + ' → ' + to, 'd-inline font-italic clearfix');
        }
    }
}