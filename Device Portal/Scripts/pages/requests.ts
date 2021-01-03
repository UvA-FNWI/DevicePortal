function page_requests(parameters: string) {
    let state = ks.local_persist('page_requests', {
        device: <Device>null,
        security_check: <SecurityCheck>null,
        checks: <SecurityCheck[]>[],
        search: { name: '', institute: '' },
    });
    if (isPageSwap) {
        state.device = null;
        state.search.name = '';
        state.search.institute = '';

        GET_ONCE('security_checks', API.SecurityCheck()).done((checks: SecurityCheck[]) => {
            state.checks = checks;
            ks.refresh();
        });
        return; // wait for security checks
    }

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
        ks.text('Firstname Lastname', 'mt-n3 mb-3 text-muted');
    }


    if (!state.device) {
        ks.set_next_item_class_name('bg-white border');
        ks.table('devices', function () {
            ks.table_body(function () {
                for (let i = 0; i < state.checks.length; ++i) {
                    let c = state.checks[i];

                    ks.table_row(function () {
                        ks.table_cell(c.userName); // TODO User fullname
                        ks.table_cell(c.userName);
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

        ks.button('Reject', ks.no_op, 'danger mr-2');
        ks.button('Approve', ks.no_op, 'success');
    }
}