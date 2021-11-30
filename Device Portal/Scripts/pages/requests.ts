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
            search: { department: '', user: '', device: '' },
        });
        if (isPageSwap) {
            state.device = null;
            state.search.user = '';
            state.search.device = '';
            state.search.department = '';

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
            if (state.checks.length === 0) {
                ks.alert_box('no-checks', 'info', true, function () {
                    ks.text('There are no submitted checks available at this moment.');
                });
            }

            ks.set_next_item_class_name('bg-white border');
            ks.table('devices', function () {
                ks.table_head(function () {
                    ks.table_row(function () {
                        ks.table_cell(function () {
                            ks.input_text('institute', state.search.department, 'Institute', function (str) {
                                state.search.department = str;
                                ks.refresh();
                            });
                            ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                        }, ks.Sort_Order.none);
                        ks.table_cell(function () {
                            ks.input_text('user', state.search.user, 'User', function (str) {
                                state.search.user = str;
                                ks.refresh();
                            });
                            ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                        }, ks.Sort_Order.none);
                        ks.table_cell(function () {
                            ks.input_text('device', state.search.device, 'Device', function (str) {
                                state.search.device = str;
                                ks.refresh();
                            });
                            ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                        }, ks.Sort_Order.none);
                        ks.table_cell('');
                    });
                });

                let search = { department: '', user: '', device: '' };
                for (let key in state.search) {
                    if (typeof state.search[key] === 'string') { search[key] = state.search[key].toUpperCase(); }
                }

                ks.table_body(function () {
                    for (let i = 0; i < state.checks.length; ++i) {
                        let c = state.checks[i];

                        if (search.department && c.departmentName.toUpperCase().indexOf(search.department) < 0 ||
                            search.user && c.userFullName.toUpperCase().indexOf(search.user) < 0 ||
                            search.device && c.deviceName.toUpperCase().indexOf(search.device) < 0) {
                            continue;
                        }

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
            }, function (i_head, order) {
                if (i_head === 0) { state.checks.sort((a, b) => order * sort_string(a.departmentName, b.departmentName)); }
                if (i_head === 1) { state.checks.sort((a, b) => order * sort_string(a.userFullName, b.userFullName)); }
                if (i_head === 2) { state.checks.sort((a, b) => order * sort_string(a.deviceName, b.deviceName)); }
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

                    ks.navigate_to('Requests', pages[Page.Requests]);
                }, function (fail) {
                    contextModal.showWarning(fail.responseText);
                    ks.navigate_to('Requests', pages[Page.Requests]);
                });
            }
        }
    }
}
