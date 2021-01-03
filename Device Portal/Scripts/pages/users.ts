class User {
    userName: string;
    name: string;
    institute: string;
    canSecure: boolean;
    canApprove: boolean;
}

function page_users(parameters: string) {
    let state = ks.local_persist('page_users', {
        devices: <Device[]>[],
        users: <User[]>null,
        user: <User>null,
        search: { name: '', institute: '' },
    });
    if (isPageSwap) {
        state.user = null;
        state.search.name = '';
        state.search.institute = '';

        GET_ONCE('get_users', API.Users()).done((users) => {
            state.users = users;
            ks.refresh();
        });
        return; // wait for users
    }

    if (parameters) {
        let parts = parameters.split('/');
        let userId = parts[0];
        if (userId && (!state.user || state.user.userName != userId)) {

            $.when(
                GET(API.Users(userId)).then((user) => {
                    state.user = user;
                    ks.refresh();
                }, (fail) => {
                    if (fail.status === 404) { contextModal.showWarning("User not found"); }
                    ks.navigate_to('Users', pages[Page.Users])
                }),
                GET(API.Devices(`User/${userId}`)).done(devices => {
                    state.devices = devices;
                })).always(() => {
                    ks.refresh();
                });

            return; // wait for user
        }
    } else { state.user = null; }

    let breadcrumbs = ['Users'];
    if (state.user) { breadcrumbs.push(state.user.name); }
    header_breadcrumbs(breadcrumbs, function () {
        ks.navigate_to('Users', pages[Page.Users]);
    });

    if (!state.user) {
        ks.set_next_item_class_name('mx-n2');
        let stats = ks.row('stats', function () {
            ks.column('users', '12 col-sm-6 col-md-3 px-2', function () {
                ks.group('card', 'card mb-3', function () {
                    ks.group('body', 'card-body text-center d-flex flex-column justify-content-center', function () {
                        ks.icon('fa fa-users').style.fontSize = '1.5rem';
                        ks.h4(state.users.length.toString(), 'font-weight-bolder text-secondary mt-2 mb-2');
                        ks.text('Users', 'text-muted');
                    });
                });
            });

            ks.column('auth', '12 col-sm-6 col-md-3 px-2', function () {
                ks.group('card', 'card mb-3', function () {
                    ks.group('body', 'card-body text-center d-flex flex-column justify-content-center', function () {
                        ks.icon('fa fa-list-ol').style.fontSize = '1.5rem';
                        let count = state.users.reduce((count, u) => count + (u.canSecure ? 1 : 0), 0);
                        ks.h4(count.toString(), 'font-weight-bolder text-secondary mt-2 mb-2');
                        ks.text('Authorized', 'text-muted');
                    });
                });
            });

            ks.column('approvers', '12 col-sm-6 col-md-3 px-2', function () {
                ks.group('card', 'card mb-3', function () {
                    ks.group('body', 'card-body text-center d-flex flex-column justify-content-center', function () {
                        ks.icon('fa fa-gavel').style.fontSize = '1.5rem';
                        let count = state.users.reduce((count, u) => count + (u.canApprove ? 1 : 0), 0);
                        ks.h4(count.toString(), 'font-weight-bolder text-secondary mt-2 mb-2');
                        ks.text('Approvers', 'text-muted');
                    });
                });
            });

            ks.column('devices', '12 col-sm-6 col-md-3 px-2', function () {
                ks.group('card', 'card mb-3', function () {
                    ks.group('body', 'card-body text-center d-flex flex-column justify-content-center', function () {
                        ks.icon('fa fa-microchip').style.fontSize = '1.5rem';
                        ks.h4('5', 'font-weight-bolder text-secondary mt-2 mb-2');
                        ks.text('Devices', 'text-muted');
                    });
                });
            });
        });

        ks.row('users', function () {
            ks.set_next_item_class_name('mb-3');
            ks.column('users', 12, function () {
                ks.set_next_item_class_name('bg-white border');
                ks.table('users', function () {
                    ks.table_head(function () {
                        ks.table_row(function () {
                            ks.table_cell(function () {
                                ks.set_next_item_class_name('form-control-sm');
                                ks.input_text('name', state.search.name, 'Name', function (str) {
                                    state.search.name = str;
                                    ks.refresh();
                                });
                                ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                            }, ks.Sort_Order.none);

                            ks.table_cell(function () {
                                ks.set_next_item_class_name('form-control-sm');
                                ks.input_text('institute', state.search.institute, 'Institute', function (str) {
                                    state.search.institute = str;
                                    ks.refresh();
                                });
                                ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                            }, ks.Sort_Order.none);

                            ks.set_next_item_class_name('text-nowrap');
                            ks.table_cell('Approver', ks.Sort_Order.none).style.width = '1%';

                            ks.set_next_item_class_name('text-nowrap');
                            ks.table_cell('Authorized', ks.Sort_Order.none).style.width = '1%';
                        });
                    });

                    let searchName = state.search.name.toLowerCase();
                    let searchInst = state.search.institute.toLowerCase();
                    ks.table_body(function () {
                        for (let i = 0; i < state.users.length; ++i) {
                            let u = state.users[i];

                            // Note: can optimize this by preprocessing lowercase for names
                            if (searchName) {
                                if (u.name.toLowerCase().indexOf(searchName) < 0) { continue; }
                            }
                            if (searchInst) {
                                if (u.institute.toLowerCase().indexOf(searchInst) < 0) { continue; }
                            }

                            ks.table_row(function () {
                                ks.push_id(i.toString());

                                ks.set_next_item_class_name('cursor-pointer');
                                ks.table_cell(u.name);

                                ks.set_next_item_class_name('cursor-pointer');
                                ks.table_cell(u.institute);

                                ks.table_cell(function () {
                                    ks.switch_button('##approve', u.canApprove, function (checked) {
                                        u.canApprove = checked;
                                        ks.refresh(stats);
                                    });
                                });
                                ks.is_item_clicked(function (_, ev) { ev.stopPropagation() });

                                ks.table_cell(function () {
                                    ks.switch_button('##secure', u.canSecure, function (checked) {
                                        u.canSecure = checked;
                                        ks.refresh(stats);
                                    });
                                });
                                ks.is_item_clicked(function (_, ev) { ev.stopPropagation() });

                                ks.pop_id();
                            });
                            ks.is_item_clicked(function () {
                                ks.navigate_to(u.name, '/users/' + u.userName);
                            });
                        }
                    });
                }, function (i_head, order) {
                    if (i_head === 0) { state.users.sort((a, b) => order * sort_string(a.name, b.name)); }
                    if (i_head === 1) { state.users.sort((a, b) => order * sort_string(a.institute, b.institute)); }
                    if (i_head === 2) { state.users.sort((a, b) => order * sort_bool(a.canApprove, b.canApprove)); }
                    if (i_head === 3) { state.users.sort((a, b) => order * sort_bool(a.canSecure, b.canSecure)); }
                });
            });
        });
    } else {
        ks.text(state.user.institute, 'mt-n3 mb-3 text-muted');

        ks.row(state.user.userName.toString(), function () {
            ks.set_next_item_class_name('mb-3');
            ks.column('devices', 12, function () {
                ks.set_next_item_class_name('bg-white border');
                ks.table('devices', function () {
                    ks.table_body(function () {
                        for (let i = 0; i < state.devices.length; ++i) {
                            device_row(state.devices[i], false);
                        }
                    });
                });
            });
        });
    }
}

let collator = new Intl.Collator('en', { sensitivity: 'base' });

function sort_string(a: string, b: string) {
    return collator.compare(a, b);
}

function sort_bool(a: boolean, b: boolean) {
    return a && !b ? -1 : (b && !a ? 1 : 0);
}