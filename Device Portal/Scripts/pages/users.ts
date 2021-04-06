namespace DP {
    class User {
        userName: string;
        name: string;
        nameLowerCase: string;
        institute: string;
        instituteLowerCase: string;
        canSecure: boolean;
        canApprove: boolean;
    }
    class UserSearchParams {
        name = '';
        institute = '';
    }

    export function page_users(parameters: string) {
        let state = ks.local_persist('page_users', {
            devices: <Device[]>[],
            deviceCount: <number>null,
            users: <User[]>null,
            user: <User>null,
            search: new UserSearchParams(),
        });
        if (isPageSwap) {
            state.user = null;
            state.search.name = '';
            state.search.institute = '';

            $.when(
                GET(API.Users()).done((users) => {
                    for (let i = 0; i < users.length; ++i) {
                        users[i].nameLowerCase = users[i].name ? users[i].name.toLowerCase() : '';
                        users[i].instituteLowerCase = users[i].institute ? users[i].institute.toLowerCase() : '';
                    }
                    users.sort((a, b) => sort_string(a.name, b.name));
                    state.users = users;
                }),
                GET(API.Devices('count')).done(count => {
                    state.deviceCount = count;
                }))
                .always(function () { ks.refresh(); });
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
                    })).always(function () { ks.refresh(); });
            }
            if (!state.user) { return; } // wait for user
        } else { state.user = null; }

        if (!state.users) { return; } // wait for users

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
                            ks.h4(state.deviceCount.toString(), 'font-weight-bolder text-secondary mt-2 mb-2');
                            ks.text('Devices', 'text-muted');
                        });
                    });
                });
            });

            ks.row('users', function () {
                ks.set_next_item_class_name('mb-3');
                ks.column('users', 12, function () {
                    let count = 0;

                    for (let i = 0; i < state.users.length; ++i) {
                        if (!user_search_match(state.search, state.users[i])) { continue; }
                        ++count;
                    }

                    let range = paginator_range('paginator', count);

                    ks.set_next_item_class_name('bg-white border mb-2');
                    ks.table('users', function () {
                        ks.table_head(function () {
                            ks.table_row(function () {
                                ks.table_cell(function () {
                                    ks.set_next_item_class_name('form-control-sm');
                                    ks.input_text('name', state.search.name, 'Name', function (str) {
                                        state.search.name = str;
                                        range.reset();
                                        ks.refresh();
                                    });
                                    ks.is_item_clicked(function (_, ev) { ev.stopPropagation(); });
                                }, ks.Sort_Order.asc);

                                ks.table_cell(function () {
                                    ks.set_next_item_class_name('form-control-sm');
                                    ks.input_text('institute', state.search.institute, 'Institute', function (str) {
                                        state.search.institute = str;
                                        range.reset();
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


                        ks.table_body(function () {
                            let countdown = range.i_end - range.i_start;
                            for (let i = range.i_start; countdown > 0; ++i) {
                                let u = state.users[i];
                                if (!user_search_match(state.search, u)) { continue; }
                                --countdown;

                                ks.table_row(function () {
                                    ks.set_next_item_class_name('cursor-pointer');
                                    ks.table_cell(u.name);

                                    ks.set_next_item_class_name('cursor-pointer');
                                    ks.table_cell(u.institute);

                                    ks.table_cell(function () {
                                        ks.switch_button('##approve', u.canApprove, function (checked) {
                                            u.canApprove = checked;
                                            PUT_JSON(API.Users(u.userName), u);
                                            ks.refresh(stats);
                                        });
                                    });
                                    ks.is_item_clicked(function (_, ev) { ev.stopPropagation() });

                                    ks.table_cell(function () {
                                        ks.switch_button('##secure', u.canSecure, function (checked) {
                                            u.canSecure = checked;
                                            PUT_JSON(API.Users(u.userName), u);
                                            ks.refresh(stats);
                                        });
                                    });
                                    ks.is_item_clicked(function (_, ev) { ev.stopPropagation() });
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

                    ks.set_next_item_class_name('ml-1');
                    paginator('paginator', state.users.filter(u => user_search_match(state.search, u)).length, () => ks.refresh(this));
                });
            });
        } else {
            ks.text(state.user.institute, 'mt-n3 mb-3 text-muted');

            ks.row(state.user.userName, function () {
                ks.set_next_item_class_name('mb-3');
                ks.column('devices', 12, function () {
                    ks.set_next_item_class_name('bg-white border');
                    ks.table('devices', function () {
                        device_table_head(0);
                        ks.table_body(function () {
                            for (let i = 0; i < state.devices.length; ++i) {
                                device_row(state.devices[i], DTF.EditNote, '');
                            }
                        });
                    });
                });
            });
        }
    }


    function user_search_match(p: UserSearchParams, u: User) {    
        return (!p.name || u.nameLowerCase.indexOf(p.name) >= 0) &&
            (!p.institute || u.instituteLowerCase.indexOf(p.institute) >= 0);
    }
}