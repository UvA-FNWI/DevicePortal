let questions = [
    { text: 'Does the device have encrypted storage?', mask: DeviceType.All, },
    { text: 'Are local accounts only accessible with strong passwords?', mask: DeviceType.All },
    { text: 'Does the device have a strong access code (minimum 6 characters) other than the SIM card PIN code?', mask: DeviceType.Mobile | DeviceType.Tablet },
    { text: 'The OS and all applications are maintained by a supplier or community, and are up to date including security updates.', mask: DeviceType.All },
];

function page_security_check(parameters: string) {
    let state = ks.local_persist('page_security_check', {
        checks: <boolean[]>[],
        device: <Device>null,
    });
    if (isPageSwap) {
        // TODO: get checks from server
        for (let i = 0; i < state.checks.length; ++i) {
            state.checks[i] = undefined;
        }
        state.device = null;
    }

    if (parameters && parameters !== 'add') {
        let parts = parameters.split('/');
        let deviceId = parseInt(parts[0]);
        if (!isNaN(deviceId)) {
            // TODO: get device from server
            state.device = { ...devices[deviceId] };
        }
    }
    if (!state.device) {
        ks.navigate_to('Home', '/');
        return;
    }

    header_breadcrumbs(['Security check'], ks.no_op);


    ks.form('security', '', false, function () {
        for (let i = 0; i < questions.length; ++i) {
            let q = questions[i];
            if (!(q.mask & state.device.type)) { continue; }

            ks.push_id(i.toString());

            ks.text(q.text, 'font-weight-bold mb-1');
            ks.group('radios', 'mb-3', function () {
                ks.set_next_item_class_name('custom-control-inline');
                ks.radio_button('Yes', state.checks[i] === true, function () {
                    state.checks[i] = true;
                    ks.refresh(this);
                });

                ks.set_next_input_validation(state.checks[i] !== undefined, '', 'Must select a value');
                ks.set_next_item_class_name('custom-control-inline');
                ks.radio_button('No', state.checks[i] === false, function () {
                    state.checks[i] = false;
                    ks.refresh(this);
                });
            });

            ks.pop_id();
        }

        ks.button('Submit', function () { });

        if (ks.current_form_submitted()) {
            if (this.getElementsByClassName('is-invalid').length) {
                ks.cancel_current_form_submission();
            } else {
                ks.navigate_to('Home', '/');
                devices[state.device.id].status = 'Submitted';
            }
        }
    });
}