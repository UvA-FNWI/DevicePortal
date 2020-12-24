function page_requests(parameters: string) {
    let state = ks.local_persist('page_users', {
        device: <Device>null,
        search: { name: '', institute: '' },
    });
    if (isPageSwap) {
        state.device = null;
        state.search.name = '';
        state.search.institute = '';
    }

    if (parameters) {
        let parts = parameters.split('/');
        let deviceId = parseInt(parts[0]);
        if (!isNaN(deviceId)) {
            // TODO: get device from server
            state.device = devices[deviceId];
        } else { state.device = null; }
    } else { state.device = null; }

    let breadcrumbs = ['Requests'];
    if (state.device) { breadcrumbs.push(state.device.name); }
    header_breadcrumbs(breadcrumbs, function () {
        ks.navigate_to('Requests', pages[Page.Requests]);
    });

    ks.row('requests', function () {
        ks.set_next_item_class_name('mb-3');
        ks.column('devices', 12, function () {
            ks.set_next_item_class_name('bg-white border');
            ks.table('devices', function () {
                ks.table_body(function () {
                    for (let i = 0; i < devices.length; ++i) {
                        let d = devices[i];

                        ks.table_row(function () {
                            ks.table_cell(users[2].name);
                            ks.table_cell(d.name);
                            ks.table_cell(function () {
                                ks.set_next_item_class_name('text-nowrap');
                                ks.anchor('View', pages[Page.SecurityCheck] + '/' + d.id);
                                ks.is_item_clicked(function () {
                                    ks.navigate_to('Security check', pages[Page.SecurityCheck] + '/' + d.id);
                                    return false;
                                });
                            }).style.width = '1%';
                        });
                    }
                });
            });
        });
    });
}