class Institute {
    id: number;
    name: string;
    users: number;
    usersAuthorized: number;
    usersApprover: number;
    usersIntuneCompleted: number;
    usersCheckSubmitted: number;
    usersCheckApproved: number;
    devices: number;
    devicesBYOD: number;
    devicesManaged: number;
    devicesSelfSupport: number;
    devicesIntuneCompleted: number;
    devicesCheckSubmitted: number;
    devicesCheckApproved: number;
}
class Faculty {
    users: number;
    usersAuthorized: number;
    usersApprover: number;
    departments: Institute[];
}
function page_faculty(parameters: string) {
    let state = ks.local_persist('page_faculty', {
        faculty: <Faculty>undefined,
        institutes: <Institute[]>[],
    });

    header_breadcrumbs(['Faculty'], ks.no_op);

    if (isPageSwap) {
        GET_ONCE('get_faculties', API.Faculties()).done((faculty: Faculty) => {
            state.faculty = faculty;
            state.institutes = faculty.departments.sort((a, b) => sort_string(a.name, b.name));
            ks.refresh();
        });
        return; // wait for institutes
    }

    ks.set_next_item_class_name('mx-n2');
    ks.row('stats', function () {
        ks.column('users', '12 col-sm-6 col-md-3 px-2', function () {
            ks.group('card', 'card mb-3', function () {
                ks.group('body', 'card-body text-center d-flex flex-column justify-content-center', function () {
                    ks.icon('fa fa-users').style.fontSize = '1.5rem';
                    ks.h4(state.faculty.users.toString(), 'font-weight-bolder text-secondary mt-2 mb-2');
                    ks.text('Users', 'text-muted');
                });
            });
        });

        ks.column('auth', '12 col-sm-6 col-md-3 px-2', function () {
            ks.group('card', 'card mb-3', function () {
                ks.group('body', 'card-body text-center d-flex flex-column justify-content-center', function () {
                    ks.icon('fa fa-list-ol').style.fontSize = '1.5rem';
                    ks.h4(state.faculty.usersAuthorized.toString(), 'font-weight-bolder text-secondary mt-2 mb-2');
                    ks.text('Authorized', 'text-muted');
                });
            });
        });

        ks.column('approvers', '12 col-sm-6 col-md-3 px-2', function () {
            ks.group('card', 'card mb-3', function () {
                ks.group('body', 'card-body text-center d-flex flex-column justify-content-center', function () {
                    ks.icon('fa fa-gavel').style.fontSize = '1.5rem';
                    ks.h4(state.faculty.usersApprover.toString(), 'font-weight-bolder text-secondary mt-2 mb-2');
                    ks.text('Approvers', 'text-muted');
                });
            });
        });

        ks.column('devices', '12 col-sm-6 col-md-3 px-2', function () {
            ks.group('card', 'card mb-3', function () {
                ks.group('body', 'card-body text-center d-flex flex-column justify-content-center', function () {
                    ks.icon('fa fa-microchip').style.fontSize = '1.5rem';
                    let count = state.institutes.reduce((count, i) => count + i.devices, 0);
                    ks.h4(count.toString(), 'font-weight-bolder text-secondary mt-2 mb-2');
                    ks.text('Devices', 'text-muted');
                });
            });
        });
    });

    ks.set_next_item_class_name('mx-n2');
    ks.row('faculties', function () {
        for (let i = 0; i < state.institutes.length; ++i) {
            let inst = state.institutes[i];

            ks.column(i.toString(), '12 col-md-6 px-2', function () {
                ks.group(inst.name, 'card mb-3', function () {
                    ks.group('body', 'card-body', function () {
                        let url = pages[Page.Institute] + '/' + inst.id;
                        ks.anchor(inst.name, url, function () {
                            ks.h5(inst.name, 'card-title');
                        });
                        ks.is_item_clicked(function (_, ev) {
                            ks.navigate_to(inst.name, url);
                            ev.stopPropagation();
                        });

                        ks.group('devices', 'mb-1 d-flex', function () {
                            ks.group('total', '', function () {
                                ks.icon('fa fa-microchip mr-1 d-inline-block').style.width = '16px';
                                ks.text('Devices: ' + inst.devices, 'd-inline');
                            });

                            ks.set_next_item_class_name('ml-auto');
                            if (inst.devicesManaged) {
                                ks.group('managed', 'badge badge-primary align-self-center', function () {
                                    ks.icon('fa fa-cogs mr-1 d-inline-block');
                                    ks.text(inst.devicesManaged.toString(), 'd-inline');
                                }).addTooltip('Managed');
                            }
                            if (inst.devicesSelfSupport) {
                                ks.group('self-support', 'ml-1 badge badge-primary align-self-center', function () {
                                    ks.icon('fa fa-question-circle mr-1 d-inline-block');
                                    ks.text(inst.devicesSelfSupport.toString(), 'd-inline');
                                }).addTooltip('Self support');
                            }
                            if (inst.devicesBYOD) {
                                ks.group('byod', 'ml-1 badge badge-primary align-self-center', function () {
                                    ks.icon('fa fa-hand-paper-o mr-1 d-inline-block');
                                    ks.text(inst.devicesBYOD.toString(), 'd-inline');
                                }).addTooltip('Bring your own device');
                            }
                        });
                        
                        ks.progress_bar('device_bar_stacked', inst.devicesCheckSubmitted.toString(), inst.devicesCheckSubmitted, inst.devices, 'bg-info tt');
                        ks.progress_bar('device_bar_stacked', inst.devicesCheckApproved.toString(), inst.devicesCheckApproved, inst.devices, 'bg-success opacity-40 tt');
                        ks.progress_bar('device_bar_stacked', inst.devicesIntuneCompleted.toString(), inst.devicesIntuneCompleted, inst.devices, 'bg-success tt');
                        let bars = (ks.get_current_item() as HTMLElement).children;
                        add_tooltip(<HTMLElement>bars[0], 'Portal check submitted');
                        add_tooltip(<HTMLElement>bars[1], 'Portal check completed');
                        add_tooltip(<HTMLElement>bars[2], 'Intune completed');

                        ks.group('users', 'mb-1 mt-2 d-flex', function () {
                            ks.group('total', '', function () {
                                ks.icon('fa fa-list-ol d-inline');
                                ks.text('Users: ' + inst.users, 'ml-1 d-inline');
                            });

                            ks.set_next_item_class_name('ml-auto');
                            if (inst.usersAuthorized) {
                                ks.group('authorized', 'ml-auto badge badge-primary align-self-center', function () {
                                    ks.icon('fa fa-list-ol mr-1 d-inline-block');
                                    ks.text(inst.usersAuthorized.toString(), 'd-inline');
                                }).addTooltip('Authorized');
                            }
                            if (inst.usersApprover) {
                                ks.group('approvers', 'ml-1 badge badge-primary align-self-center', function () {
                                    ks.icon('fa fa-gavel mr-1 d-inline-block');
                                    ks.text(inst.usersApprover.toString(), 'd-inline');
                                }).addTooltip('Approvers');
                            }
                        });
                        ks.set_next_item_class_name('overflow-visible');
                        ks.progress_bar('bar_stacked', inst.usersCheckSubmitted.toString(), inst.usersCheckSubmitted, inst.users, 'bg-info tt');
                        ks.progress_bar('bar_stacked', inst.usersCheckApproved.toString(), inst.usersCheckApproved, inst.users, 'bg-success opacity-40 tt');
                        ks.progress_bar('bar_stacked', inst.usersIntuneCompleted.toString(), inst.usersIntuneCompleted, inst.users, 'bg-success tt');
                        bars = (ks.get_current_item() as HTMLElement).children;
                        add_tooltip(<HTMLElement>bars[0], 'Portal check submmitted');
                        add_tooltip(<HTMLElement>bars[1], 'Portal check authorized');
                        add_tooltip(<HTMLElement>bars[2], 'Intune completed');
                    });
                });
            });
        }
    });
}

function add_tooltip(element: HTMLElement, text: string) {
    element.title = text;
    element.setAttribute('data-toggle', 'tooltip');
    $(element).tooltip();
}
interface HTMLElement {
    addTooltip(text: string);
}
if (typeof HTMLElement.prototype.addTooltip !== 'function') {
    HTMLElement.prototype.addTooltip = function (text: string) {
        add_tooltip(this, text);
    }
}
