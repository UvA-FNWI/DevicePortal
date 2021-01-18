class Institute {
    name: string;
    users: number;
    usersCompleted: number;
    devices: number;
}

function page_faculty(parameters: string) {
    let state = ks.local_persist('page_faculty', {
        institutes: <Institute[]>[],
    });

    header_breadcrumbs(['Faculty'], ks.no_op);

    if (isPageSwap) {
        GET_ONCE('get_faculties', API.Faculties()).done((institutes: Institute[]) => {
            state.institutes = institutes.sort((a,b) => sort_string(a.name, b.name));
            ks.refresh();
        });
        return; // wait for institutes
    }

    ks.set_next_item_class_name('mx-n2');
    ks.row('faculties', function () {
        for (let i = 0; i < state.institutes.length; ++i) {
            let inst = state.institutes[i];

            ks.column(i.toString(), '12 col-md-6 px-2', function () {
                ks.group(inst.name, 'card mb-3', function () {
                    ks.group('body', 'card-body', function () {
                        let url = pages[Page.Institute] + '/' + inst.name;
                        ks.anchor(inst.name, url, function () {
                            ks.h5(inst.name, 'card-title');
                        });
                        ks.is_item_clicked(function (_, ev) {
                            ks.navigate_to(inst.name, url);
                            ev.stopPropagation();
                        });

                        ks.group('devices', 'mb-2', function () {
                            ks.icon('fa fa-microchip mr-1 d-inline-block').style.width = '16px';
                            ks.text('Registered devices: ', 'd-inline');
                            ks.text('' + inst.devices + ' ', 'd-inline');
                        });

                        ks.group('completed', 'mb-1', function () {
                            ks.icon('fa fa-list-ol d-inline');
                            ks.text('Users completed their security checks:', 'ml-1 d-inline');
                        });
                        ks.progress_bar('bar', inst.usersCompleted + ' / ' + inst.users, inst.usersCompleted, inst.users, 'bg-info');
                    });
                });
            });
        }
    });
}