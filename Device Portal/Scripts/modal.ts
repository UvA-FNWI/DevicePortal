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
    class EditDevice {
        userName = '';
        shared = false;
        notes = '';
        itracsBuilding = '';
        itracsRoom = '';
        itracsOutlet = '';

        static equal(e: EditDevice, d: Device): boolean {
            for (let key in e) {
                let ve = e[key];
                let vd = d[key];
                if (typeof (ve) === 'string') {
                    if (ve !== (vd || '')) { return false; }
                } else {
                    if (ve !== vd) { return false; }
                }
            }
            return true;
        }
    }

    export class DeviceModal {
        id = '####device_modal';
        showTimeline = false;
        edit = new EditDevice();
        user: string;
        device: Device;
        display: Device;
        history: DeviceHistory[];
        users: User[] = [];
        el: HTMLElement;
        buildings = [
            'BG1',
            'REC-G',
            'NIKHEF',
            'NIKHEF F',
            'O2 VU',
            'SP 500 (F)',
            'SP 507 (Kassen)',
            'SP 508 (G)',
            'SP 608B (Startup Village)',
            'SP 700 (E)',
            'SP 904',
            'SP 904 (A)',
            'SP 904 (ABCD)',
        ];
        comboId = 0;

        show(device: Device, showTimeline: boolean) {
            for (let key in this.edit) {
                let v = device[key];
                this.edit[key] = v != null ? v : '';
            }
            this.user = device.user?.name || '';
            this.device = device;
            this.display = device;
            this.history = null;
            this.showTimeline = showTimeline;
            ks.refresh(this.el);
            ks.open_popup(this.id);

            if (showTimeline) {
                GET_ONCE('history', API.Devices(device.id + '/History')).done((history: DeviceHistory[]) => {
                    for (let i = 0; i < history.length; ++i) {
                        let h = history[i];
                        h.dateHistory = truncTimeOffDate(h.dateHistory);

                        Device.formatPurchaseDate(h);
                        Device.formatLastSeenDate(h);
                    }
                    this.showTimeline = true;
                    this.history = history;
                    ks.refresh();
                });
            }

            GET_ONCE('user list', API.Users()).done((users: User[]) => {
                for (let i = 0; i < users.length; ++i) { 
                    users[i].name = users[i].name || '';
                }
                this.users = users;
                ks.refresh(this.el);
            });
        }

        run(activeUser: ActiveUser) {
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
                                for (let key in modal.edit) {
                                    let v = modal.device[key];
                                    modal.edit[key] = v != null ? v : '';
                                }
                                ks.refresh(modal.el);
                            });
                        });
                    }

                    ks.row('row', function () {
                        ks.column('left', 6, function () {
                            detail('Type', deviceTypes[d.type]);

                            if (is_current) {
                                ks.text('User', 'font-weight-bold');

                                ks.set_next_item_class_name('mb-2 text-center form-control-sm d-inline-block');
                                let input = ks.input_text('user', modal.user, 'User', function (val) {
                                    modal.user = val;
                                });
                                input.style.width = '200px';

                                $(input).autocomplete({
                                    treshold: 1,
                                    source: modal.users,
                                    label: 'name',
                                    value: 'userName',
                                    onSelectItem: function (item) {
                                        modal.user = item.label;
                                        modal.edit.userName = item.value;
                                        ks.refresh(modal.el);
                                    }
                                });
                            } else {
                                detail('User', d.user?.name);
                            }

                            if (!(d.category & (DeviceCategory.ManagedSpecial | DeviceCategory.ManagedStandard))) {
                                ks.text('Status', 'font-weight-bold');
                                ks.text(statusNames[d.status], 'mb-3 badge badge-' + statusColors[d.status]);
                            }

                            detail('Category', deviceCategories[d.category]);
                            detail('Cost centre', d.costCentre);

                            if (is_current) {
                                ks.text('Building', 'font-weight-bold');
                                ks.set_next_item_class_name('custom-select-sm mb-2');
                                // make sure we recreate combo every refresh in order to 
                                // clear selections from previously opened modals
                                ks.combo('' + (++modal.comboId), function () {
                                    if (!modal.buildings.some(b => b === modal.display.itracsBuilding)) {
                                        ks.selectable(modal.display.itracsBuilding || '##empty',
                                            modal.edit.itracsBuilding === modal.display.itracsBuilding);
                                        ks.is_item_clicked(function () {
                                            modal.edit.itracsBuilding = modal.display.itracsBuilding || '';
                                            ks.refresh(modal.el);
                                        });
                                    }

                                    for (let i = 0; i < modal.buildings.length; ++i) {
                                        let building = modal.buildings[i];
                                        ks.selectable(building, building === modal.edit.itracsBuilding);
                                        ks.is_item_clicked(function () {
                                            modal.edit.itracsBuilding = building;
                                            ks.refresh(modal.el);
                                        });
                                    }
                                }).style.width = '200px';
                            } else {
                                detail('Building', d.itracsBuilding);
                            }

                            if (is_current) {
                                ks.text('Room', 'font-weight-bold');
                                ks.set_next_item_class_name('mb-2 text-center form-control-sm d-inline-block');
                                ks.input_text('Room', modal.edit.itracsRoom, 'Room', function (val) {
                                    modal.edit.itracsRoom = val;
                                    ks.refresh(modal.el);
                                }).style.width = '200px';
                            } else {
                                detail('Room', modal.display.itracsRoom);
                            }

                            if (is_current) {
                                ks.text('Outlet', 'font-weight-bold');
                                ks.set_next_item_class_name('mb-2 text-center form-control-sm d-inline-block');
                                ks.input_text('Outlet', modal.edit.itracsOutlet, 'Outlet', function (val) {
                                    modal.edit.itracsOutlet = val;
                                    ks.refresh(modal.el);
                                }).style.width = '200px';
                            } else {
                                detail('Outlet', modal.display.itracsOutlet);
                            }
                        });

                        ks.column('right', 6, function () {
                            detail('Device ID', d.deviceId);

                            ks.text('Shared device', 'font-weight-bold');
                            ks.set_next_item_class_name('mb-3' + (is_current ? '' : ' disabled'));
                            ks.switch_button('Multiple users', modal.edit.shared, function (checked) {
                                modal.edit.shared = checked;
                                ks.refresh(modal.el);
                            }).disabled = !is_current;

                            detail('Serial number', d.serialNumber);
                            detail('MAC address', d.macadres);
                            detail('IPv4', d.ipv4);
                            detail('IPv6', d.ipv6);
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
                        ks.input_text_area('note', modal.edit.notes,
                            'Add a note to this device. This note is visible to the device owner.', function (str) {
                                modal.edit.notes = str;
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
                                    h.dateHistory = truncTimeOffDate(h.dateHistory);

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
                            ks.close_current_popup();
                        }, 'outline-secondary mr-2');

                        let disabled = modal.device !== modal.display || EditDevice.equal(modal.edit, d);
                        ks.button('Save', function () {
                            let prev = new EditDevice();
                            for (let key in modal.edit) {
                                prev[key] = d[key];
                                d[key] = modal.edit[key];
                            }

                            let entity: any = {};
                            for (let key in d) { entity[key] = d[key]; }
                            // These dates are modified for display purposes, but could be unparsable by back-end
                            entity.lastSeenDate = null;
                            entity.purchaseDate = null;

                            ks.close_current_popup();

                            PUT_JSON(API.Devices(d.id), entity).then(() => {
                                if (modal.device.userName !== prev.userName) {
                                    let index = modal.users.findIndex(u => u.userName == modal.device.userName);
                                    if (index >= 0) { modal.device.user = modal.users[index]; }
                                }

                                modal.device.userEditId = activeUser.user_name;
                                modal.device.userEditName = activeUser.first_name + ' ' + activeUser.last_name;

                                contextModal.showSuccess('Changes successfully saved.');
                                ks.refresh();
                            }, fail => {
                                for (let key in prev) { d[key] = prev[key]; }
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
                ks.group('##most recent', modal.display !== modal.device ? 'timeline-item cursor-pointer' : 'timeline-item', function () {
                    ks.icon('fa fa-clock-o timeline-icon bg-primary text-light');
                    ks.group('content', 'timeline-item-content', function () {
                        let date = truncTimeOffDate(modal.device.dateEdit);
                        let title = modal.history?.length ? 'Most recent (' + date + ')': date;
                        ks.text(title, modal.device === modal.display ? 'font-weight-bold text-primary' : 'font-weight-bold');
                        modal.timeline_diff(modal.history?.length ? modal.history[0] : modal.device, modal.device);
                        let label = !modal.history?.length ? 'Added by ' : 'Edited by ';
                        ks.text(label + (modal.device.userEditName || '<unknown>'), 'text-muted').style.fontSize = '0.8rem';
                    });
                });
                ks.is_item_clicked(function () {
                    modal.display = modal.device;
                    modal.user = modal.device.user?.name || '';
                    for (let key in modal.edit) {
                        let v = modal.device[key];
                        modal.edit[key] = v != null ? v : '';
                    }
                    ks.refresh(modal.el);
                });

                if (!modal.history) { return; }

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
                        for (let key in modal.edit) {
                            let v = h[key];
                            modal.edit[key] = v != null ? v : '';
                        }
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
            diff('User: ', p.user?.name, c.user?.name);

            if (p.status !== c.status) {
                ks.group('diff status', '', function () {
                    ks.text('Status: ', 'd-inline');
                    ks.text(statusNames[p.status], 'd-inline-block badge badge-' + statusColors[p.status]);
                    ks.text(' → ', 'd-inline');
                    ks.text(statusNames[c.status], 'badge badge-' + statusColors[c.status]);
                });
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
        if (from === null && to === '') { return; }

        if (from !== to) {
            ks.text(label, 'd-inline');
            let text = from == null ? ' → ' + to : from + ' → ' + to;
            ks.text(text, 'd-inline font-italic clearfix');
        }
    }
}