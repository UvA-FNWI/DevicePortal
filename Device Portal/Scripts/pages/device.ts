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

enum OSType {
    Android = 1 << 0,
    iOS = 1 << 1,
    Linux = 1 << 2,
    MacOS = 1 << 3,
    Windows = 1 << 4,
    All = (1 << 5) - 1,
}
let osNames = {};
osNames[OSType.Android] = 'Android';
osNames[OSType.iOS] = 'iOS';
osNames[OSType.Linux] = 'Linux';
osNames[OSType.MacOS] = 'MacOS';
osNames[OSType.Windows] = 'Windows';

let deviceOS = {};
deviceOS[DeviceType.Mobile] = [OSType.Android, OSType.iOS];
deviceOS[DeviceType.Tablet] = [OSType.Android, OSType.iOS];
deviceOS[DeviceType.Laptop] = [OSType.Linux, OSType.MacOS, OSType.Windows];
deviceOS[DeviceType.Desktop] = [OSType.Linux, OSType.MacOS, OSType.Windows];
deviceOS[DeviceType.All] = [OSType.Android, OSType.iOS, OSType.Linux, OSType.MacOS, OSType.Windows];

class Device {
    id: number;
    name = '';
    nameLowerCase: string; // Note: used on institue page for performance
    deviceId: string;
    deviceIdLowerCase: string;  // Note: used on institue page for performance
    origin: DeviceOrigin;
    serialNumber: string;
    serialNumberLowerCase: string;   // Note: used on institue page for performance
    type: DeviceType;
    os_type: OSType;
    os_version: string = '';
    status = DeviceStatus.Unsecure;

    user: string; // Note(Joshua): only available in institute/devices
    userLowerCase: string; // Note: used on institue page for performance
    userName: string;
}
enum DeviceOrigin {
    DataExport,
    Intune,
    User,
}
let originNames = {};
originNames[DeviceOrigin.DataExport] = "CMDB";
originNames[DeviceOrigin.Intune] = "Intune";
originNames[DeviceOrigin.User] = "User";
enum DeviceStatus {
    Approved,
    Rejected,
    Submitted,
    Unsecure,
}
let statusNames = {};
statusNames[DeviceStatus.Approved] = 'Approved';
statusNames[DeviceStatus.Rejected] = 'Rejected';
statusNames[DeviceStatus.Submitted] = 'Pending';
statusNames[DeviceStatus.Unsecure] = 'Unsecure';
let statusColors = {};
statusColors[DeviceStatus.Approved] = 'success';
statusColors[DeviceStatus.Rejected] = 'danger';
statusColors[DeviceStatus.Submitted] = 'info';
statusColors[DeviceStatus.Unsecure] = 'warning';

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
        update: false,
        loaded_os_type: <OSType>0,
    });

    if (isPageSwap) {
        state.selected = -1;
        state.device = new Device();
    }

    if (parameters && parameters !== 'add') {
        let parts = parameters.split('/');
        let deviceId = parseInt(parts[0]);
        if (isNaN(deviceId)) {
            ks.navigate_to('Home', '/');
            return;
        }
        if (state.device.id !== deviceId && deviceId) {
            GET_ONCE('device', API.Devices(deviceId)).done((d: Device) => {
                state.update = true;
                state.device = d;
                state.loaded_os_type = d.os_type;
                if (state.device.os_version == null) { state.device.os_version = ''; }
                let mask = d.type;
                state.selected = -1;
                while (mask) {
                    ++state.selected;
                    mask = mask >> 1;
                }
                ks.refresh();
            });
            return; // wait for device
        }
    } else if (parameters === 'add') {
        state.update = false;
    } else if (!parameters) {
        ks.navigate_to('Home', '/');
        return;
    }

    header_breadcrumbs([state.device.id ? 'Edit device' : 'Add device'], ks.no_op);

    ks.row('row', function () {
        for (let i = 0; i < state.options.length; ++i) {
            let o = state.options[i];

            ks.column(i.toString(), '12 col-md-3 col-sm-6 col-xs-12', function () {
                ks.set_next_item_class_name('shadow-sm border-0 p-3 align-items-center ' +
                    (state.selected === i ? 'bg-primary text-light' : 'bg-white') +
                    (state.update ? '' : ' cursor-pointer'));
                ks.card('card', function () {
                    ks.icon(o.icon).style.fontSize = '5rem';
                    ks.text(deviceNames[o.type]);
                });
                if (!state.update) {
                    ks.is_item_clicked(function () {
                        state.selected = i;
                        state.device.type = state.options[state.selected].type;
                        ks.refresh();
                    });
                }
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
                ks.set_next_input_validation(!!state.device.os_type, '', 'This is a required field.');
                ks.combo('Operating system', function () {
                    ks.selectable('##none', !state.device.os_type);
                    ks.is_item_clicked(function () {
                        state.device.os_type = 0;
                    });
                    if (state.device.type) {
                        for (let i = 0; i < deviceOS[state.device.type].length; ++i) {
                            let type: OSType = deviceOS[state.device.type][i];
                            ks.selectable(osNames[type], state.device.os_type === type);
                            ks.is_item_clicked(function () {
                                state.device.os_type = type;
                            });
                        }
                    }
                }).disabled = state.update && !!(state.loaded_os_type & (OSType.Android | OSType.iOS));

                ks.text('Version', 'mt-2 mb-1');
                ks.set_next_input_validation(!!state.device.os_version.length, '', 'This is a required field.');
                ks.input_text('version', state.device.os_version, 'Version', function (val) {
                    state.device.os_version = val;
                });
                
                ks.group('right', 'd-flex', function () {
                    ks.set_next_item_class_name('ml-auto');
                    ks.button(state.update ? 'Update' : 'Add', ks.no_op);
                });

                if (ks.current_form_submitted() && state.selected >= 0 && !this.getElementsByClassName('is-invalid').length) {
                    ks.cancel_current_form_submission();

                    if (state.update) {
                        PUT_JSON(API.Devices(state.device.id), state.device).then(() => {
                            contextModal.showSuccess('Changes successfully saved.');
                            ks.navigate_to('Home', '/');
                        }, fail => {
                            contextModal.showWarning(fail.responseText);
                        });                    
                    } else {
                        POST_JSON(API.Devices(), state.device).then(() => {
                            contextModal.showSuccess('Device successfully added.');
                            ks.navigate_to('Home', '/');
                        }, fail => {
                            contextModal.showWarning(fail.responseText);
                        });                    
                    }
                }
            });
        });
    });
}