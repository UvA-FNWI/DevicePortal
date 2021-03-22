namespace DP {
    class SecurityCheckSubmitted {
        id: number;
        userName: string;
        userFullName: string;
        deviceName: string;
        departmentName: string;
    }

    export function page_requests(parameters: string) {
        let state = ks.local_persist('page_requests', {
            device: <Device>null,
            security_check: <SecurityCheck>null,
            checks: <SecurityCheckSubmitted[]>null,
            search: { name: '', institute: '' },
        });
        if (isPageSwap) {
            state.device = null;
            state.search.name = '';
            state.search.institute = '';

            // TODO sort
            GET_ONCE('security_checks', API.SecurityCheck('Submitted')).then((checks: SecurityCheckSubmitted[]) => {
                state.checks = checks;
                ks.refresh();
            }, (fail) => {
                if (fail.status === 403) { ks.navigate_to('Users', pages[Page.Home]) }
                else { contextModal.showWarning(fail.responseText); }
            });
        }
        if (!state.checks) { return; } // wait for security checks

        if (parameters) {
            let parts = parameters.split('/');
            let requestId = parseInt(parts[0]);
            if (!isNaN(requestId) && (!state.security_check || state.security_check.id != requestId)) {

                GET_ONCE('get_security_check', API.SecurityCheck(requestId)).then(c => {
                    state.security_check = c;
                    state.device = c.device;
                    ks.refresh();
                }, fail => {
                    if (fail.status === 404) { contextModal.showWarning("Security check not found"); }
                    ks.navigate_to('Users', pages[Page.Requests])
                });

                return; // wait for get device & security check
            }
        } else { state.device = null; state.security_check = null; }

        let breadcrumbs = ['Requests'];
        if (state.device) { breadcrumbs.push(state.device.name); }
        header_breadcrumbs(breadcrumbs, function () {
            ks.navigate_to('Requests', pages[Page.Requests]);
        });
        if (state.device) {
            ks.text(state.security_check.userDisplayName, 'mt-n3 mb-3 text-muted');
        }

        if (!state.device) {
            ks.set_next_item_class_name('bg-white border');
            ks.table('devices', function () {
                ks.table_body(function () {
                    for (let i = 0; i < state.checks.length; ++i) {
                        let c = state.checks[i];

                        ks.table_row(function () {
                            ks.table_cell(c.departmentName);
                            ks.table_cell(c.userFullName);
                            ks.table_cell(c.deviceName);
                            ks.table_cell(function () {
                                ks.set_next_item_class_name('text-nowrap');
                                ks.anchor('View', pages[Page.Requests] + '/' + c.id);
                                ks.is_item_clicked(function () {
                                    ks.navigate_to('Request', pages[Page.Requests] + '/' + c.id);
                                    return false;
                                });
                            }).style.width = '1%';
                        });
                    }
                });
            });
        } else {
            for (let i = 0; i < state.security_check.questions.length; ++i) {
                let q = state.security_check.questions[i];
                if (!(q.mask & state.device.type)) { continue; }

                ks.text(q.question, 'font-weight-bold mb-1');
                ks.group(i.toString(), 'mb-3', function () {
                    ks.set_next_item_class_name('custom-control-inline');
                    ks.radio_button('Yes', q.answer === true, ks.no_op).disabled = true;

                    ks.set_next_item_class_name('custom-control-inline');
                    ks.radio_button('No', q.answer === false, ks.no_op).disabled = true;

                    if (q.answer === false) { ks.text(q.explanation, 'mt-1 font-italic'); }
                });
            }

            ks.button('Reject', update_check_status.bind(this, DeviceStatus.Rejected), 'danger mr-2');
            ks.button('Approve', update_check_status.bind(this, DeviceStatus.Approved), 'success');

            function update_check_status(status: DeviceStatus) {
                state.security_check.status = status;
                state.device.status = status;
                PUT_JSON(API.SecurityCheck(`${state.security_check.id}/Status`), state.security_check).then(function () {
                    // Remove entry from submission list
                    const index = state.checks.findIndex(c => c.id == state.security_check.id);
                    state.checks.splice(index, 1);

                    // Update request counter in top nav
                    ks.local_persist<{ count: number }>('request count').count--;

                    contextModal.showSuccess('Succesfully saved change.');
                    ks.navigate_to('Requests', pages[Page.Requests]);
                }, function (fail) {
                    contextModal.showWarning(fail.responseText);
                    ks.navigate_to('Requests', pages[Page.Requests]);
                });
            }
        }
    }
}
