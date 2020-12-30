enum DeviceType {
    Mobile = 1 << 0,
    Tablet = 1 << 1,
    Laptop = 1 << 2,
    Desktop = 1 << 3,
    All = (1 << 4) - 1,
}
let deviceNames = {};
deviceNames[DeviceType.Mobile] = 'Mobile';
deviceNames[DeviceType.Tablet] = 'Tablet';
deviceNames[DeviceType.Laptop] = 'Laptop';
deviceNames[DeviceType.Desktop] = 'Desktop';

class Device {
    id: number;
    name = '';
    type: DeviceType;
    os = '';
    status = DeviceStatus.Submitted;
}
enum DeviceStatus {
    Approved,
    Denied,
    Submitted,
}
let statusNames = {};
statusNames[DeviceStatus.Approved] = 'Approved';
statusNames[DeviceStatus.Denied] = 'Denied';
statusNames[DeviceStatus.Submitted] = 'Pending';
let statusColors = {};
statusColors[DeviceStatus.Approved] = 'sucess';
statusColors[DeviceStatus.Denied] = 'danger';
statusColors[DeviceStatus.Submitted] = 'warning';

function page_device(parameters: string) {
    let state = ks.local_persist('page_device', {
        selected: -1,
        device: <Device>null,
        options: [
            { type: DeviceType.Mobile, icon: 'fa fa-mobile' },
            { type: DeviceType.Tablet, icon: 'fa fa-tablet' },
            { type: DeviceType.Laptop, icon: 'fa fa-laptop' },
            { type: DeviceType.Desktop, icon: 'fa fa-desktop' },
        ],
    });

    if (isPageSwap) {
        state.selected = -1;
        state.device = new Device();
    }

    if (parameters && parameters !== 'add') {
        let parts = parameters.split('/');
        let deviceId = parseInt(parts[0]);
        if (!isNaN(deviceId)) {
            // TODO: get device from server
            state.device = { ...devices[deviceId] };
        } else {
            ks.navigate_to('Home', '/');
            return;
        }
    } else if (!parameters) {
        ks.navigate_to('Home', '/');
        return;
    }

    header_breadcrumbs(['Add device'], ks.no_op);

    ks.row('row', function () {
        for (let i = 0; i < state.options.length; ++i) {
            let o = state.options[i];

            ks.column(i.toString(), '12 col-md-3 col-sm-6 col-xs-12', function () {
                ks.set_next_item_class_name('shadow-sm border-0 p-3 align-items-center cursor-pointer ' +
                    (state.selected === i ? 'bg-primary text-light' : 'bg-white'));
                ks.card('card', function () {
                    ks.icon(o.icon).style.fontSize = '5rem';
                    ks.text(deviceNames[o.type]);
                });
                ks.is_item_clicked(function () {
                    state.selected = i;
                    ks.refresh();
                });
            });
        }

        ks.column('form', 12, function () {
            ks.form('form', '', false, function () {
                if (ks.current_form_submitted() && state.selected < 0) {
                    ks.text('Please select a device type.', 'invalid-feedback d-block border-top border-danger mt-2 pt-1');
                }

                ks.text('Name', 'mt-3 mb-1');
                ks.set_next_input_validation(state.device.name.length > 0, '', 'This is a required field.');
                ks.input_text('Name', state.device.name, 'Name', function (val) {
                    state.device.name = val;
                });

                ks.text('Operating system', 'mt-2 mb-1');
                ks.set_next_input_validation(state.device.os.length > 0, '', 'This is a required field.');
                ks.combo('Operating system', function () {
                    ks.selectable('##none', !state.device.os);
                    for (let i = 0; i < operatingSystems.length; ++i) {
                        ks.selectable(operatingSystems[i], state.device.os === operatingSystems[i]);
                        ks.is_item_clicked(function () {
                            state.device.os = operatingSystems[i];
                        });
                    }
                });

                ks.group('right', 'd-flex', function () {
                    ks.set_next_item_class_name('ml-auto');
                    ks.button('Add', ks.no_op);
                });

                if (ks.current_form_submitted() && state.selected >= 0 && !this.getElementsByClassName('is-invalid').length) {
                    ks.cancel_current_form_submission();
                    devices.push({
                        id: devices.length,
                        name: state.device.name,
                        os: state.device.os,
                        type: state.options[state.selected].type,
                        status: DeviceStatus.Denied,
                    });
                    ks.navigate_to('Home', '/');
                }
            });
        });
    });
}