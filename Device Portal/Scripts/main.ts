let isPageSwap = true;

// Note: must match pages array below
enum Page {
    Device,
    SecurityCheck,
    Users,
    Faculty,
    Requests,
    Admin,
    Home, // Make sure this is last since it's pattern matches all paths
}

let pages = [
    '/device',
    '/securitycheck',
    '/users',
    '/faculty',
    '/requests',
    '/admin',
    '/',
];

let iPage = Page.Home;
let contextModal = new ContextualModal();

let devices: Device[] = []; // TODO, contains only devices registered to current user
let operatingSystems = ['Windows', 'Linux', 'macOS', 'iOS', 'Android'];

ks.run(function () {
    // Global modals, run before any pages
    contextModal.run();

    let iPagePrev = iPage;
    let pathname = window.location.pathname.toLowerCase();
    let parameters = '';
    for (let i = 0; i < pages.length; ++i) {
        if (pathname.indexOf(pages[i]) === 0) {
            iPage = i;
            parameters = pathname.substring(pages[i].length + 1);
            break;
        }
    }
    isPageSwap = isPageSwap || iPage !== iPagePrev;

    ks.nav_bar('top bar', 'navbar-expand navbar-light bg-white shadow-sm', function () {
        ks.set_next_item_class_name('navbar-nav');
        ks.group('container', 'container px-3 d-flex justify-content-between', function () {
            ks.set_next_item_class_name('navbar-nav');
            ks.unordered_list('left', '', function () {
                ks.set_next_item_class_name('navbar-brand d-flex');
                ks.anchor('brand', '/', function () {
                    ks.image('/img/uva_logo.jpg', undefined, 30, undefined, 'UvA');
                    ks.text('Science Secure', 'ml-2 d-none d-md-block');
                });
                ks.is_item_clicked(function () {
                    ks.navigate_to('Home', '/');
                    return false;
                });

                ks.nav_item('Home', iPage === Page.Home, '/');
                ks.is_item_clicked(function () {
                    ks.navigate_to('Home', '/');
                    return false;
                });

                ks.nav_item('Faculty', iPage === Page.Faculty, pages[Page.Faculty]);
                ks.is_item_clicked(function () {
                    ks.navigate_to('Faculty', pages[Page.Faculty]);
                    return false;
                });

                ks.nav_item('Users', iPage === Page.Users, pages[Page.Users]);
                ks.is_item_clicked(function () {
                    ks.navigate_to('Users', pages[Page.Users]);
                    return false;
                });

                ks.nav_item('requests', iPage === Page.Requests, pages[Page.Requests], function () {
                    ks.text('Requests', 'd-inline mr-1');
                    // TODO: get actual count
                    ks.text('12', 'badge badge-primary badge-pill');
                });
                ks.is_item_clicked(function () {
                    ks.navigate_to('Approval requests', pages[Page.Requests]);
                    return false;
                });

                ks.nav_item('Admin', iPage === Page.Users,  pages[Page.Admin]);
                ks.is_item_clicked(function () {
                    ks.navigate_to('Admin', pages[Page.Admin]);
                    return false;
                });
            });

            ks.nav_item('Logout', false, '/logout');
            ks.is_item_clicked(function () {
                GET(API.Identity('exit')).always(function () {
                    window.location.reload();
                });
                return false;
            });
        });
    });

    ks.group(pages[iPage], 'container my-3', function () {
        switch (iPage) {
            case Page.Device:
                page_device(parameters);
                break;
            case Page.SecurityCheck:
                page_security_check(parameters);
                break;
            case Page.Users:
                page_users(parameters);
                break;
            case Page.Faculty:
                page_faculty(parameters);
                break;
            case Page.Requests:
                page_requests(parameters);
                break;
            case Page.Admin:
                page_admin();
                break;
            default:
                page_home();
                break;
        }
    });

    isPageSwap = false;
});

function page_home() {
    if (isPageSwap) {
        GET_ONCE('devices', API.Devices('me')).done((result: Device[]) => {
            devices = result;
            ks.refresh();
        });
    }

    ks.h5('My devices', 'mb-2');
    ks.row('devices', function () {
        ks.set_next_item_class_name('mb-3');
        ks.column('devices', 12, function () {
            ks.set_next_item_class_name('bg-white border');
            ks.table('devices', function () {
                ks.table_body(function () {
                    for (let i = 0; i < devices.length; ++i) {
                        device_row(devices[i], true);
                    }
                });
            });

            ks.group('right', 'd-flex', function () {
                ks.set_next_item_class_name('ml-auto');
                ks.button('Add device', function () {
                    ks.navigate_to('Add device', '/device/add');
                });
            });
        });
    });
}

function deviceIcon(type: DeviceType): string {
    switch (type) {
        case DeviceType.Mobile:
            return 'fa fa-mobile';
        case DeviceType.Tablet:
            return 'fa fa-tablet';
        case DeviceType.Laptop:
            return 'fa fa-laptop';
        default:
            return 'fa fa-desktop';
    }
}

function device_row(d: Device, showSecurityCheckLink: boolean) {
    let icon: string;
    let iconSize = '1rem';
    switch (d.type) {
        case DeviceType.Mobile:
            icon = 'fa fa-mobile text-center';
            iconSize = '1.35rem';
            break;

        case DeviceType.Tablet:
            icon = 'fa fa-tablet text-center';
            iconSize = '1.2rem';
            break;

        case DeviceType.Laptop:
            icon = 'fa fa-laptop text-center';
            iconSize = '1.1rem';
            break;

        default:
            icon = 'fa fa-desktop text-center';
            break;
    }

    ks.table_row(function () {
        ks.table_cell(d.name);
        ks.table_cell(function () {
            let i = ks.icon(icon);
            i.style.width = '18px';
            i.style.fontSize = iconSize;
            ks.text(' ' + deviceNames[d.type], 'd-inline ml-1');
        });
        ks.table_cell(d.os);
        ks.table_cell(function () {
            ks.text(statusNames[d.status], 'badge badge-' + statusColors[d.status]);
        });

        if (showSecurityCheckLink) {
            ks.table_cell(function () {
                ks.set_next_item_class_name('text-nowrap');
                ks.anchor('Security check', pages[Page.SecurityCheck] + '/' + d.id);
                ks.is_item_clicked(function () {
                    ks.navigate_to('Security check', pages[Page.SecurityCheck] + '/' + d.id);
                    return false;
                });
            }).style.width = '1%';
        }
    });
}

function page_admin() {
    ks.h5('Upload files', 'mb-2');
    ks.form('form', '', true, function () {
        let files: any[];
        ks.input_file('File', '', function (f) {
            files = [f[0], f[1]];
        }).multiple = true;
        ks.button('Upload', function () {
            ks.cancel_current_form_submission();
            if (files) {
                ks.refresh(this);
                const xhr = new XMLHttpRequest();
                const fd = new FormData();
                xhr.open("POST", API.Import(), true);
                xhr.onreadystatechange = function () {
                    if (xhr.readyState === 4 && xhr.status == 400) {
                        contextModal.showWarning(xhr.responseText);
                        console.warn(xhr.responseText);
                    }
                };
                for (let i = 0; i < files.length; ++i) {
                    fd.append('my_file' + i, files[i]);
                }
                xhr.send(fd);
            }
        });
    });
}

function header_breadcrumbs(items: string[], proc: (i: number) => void) {
    let current_item = ks.get_current_parent();
    ks.group('header', 'd-flex flex-row mb-2', function () {
        ks.h5(items[items.length - 1], 'flex-grow-1');

        ks.nav('breadcrumb', '', function () {
            ks.ordered_list('items', 'breadcrumb bg-light p-0 m-0', function () {
                ks.list_item('icon', 'breadcrumb-item', function () {
                    ks.anchor('icon', '#', function () {
                        ks.icon('fa fa-home align-self-center');
                    });
                    ks.is_item_clicked(function () {
                        ks.navigate_to('Home', '/');
                        return false;
                    });
                });

                for (let i = 0; i < items.length; ++i) {
                    if (i + 1 === items.length) {
                        ks.list_item(items[i], 'breadcrumb-item active').setAttribute('aria-current', 'page');
                    } else {
                        ks.list_item('locations', 'breadcrumb-item', function () {
                            ks.anchor(items[i], '#');
                            ks.is_item_clicked(function () {
                                proc.call(current_item, i);
                                return false;
                            });
                        });
                    }
                }
            });
        }).setAttribute('aria-label', 'breadcrumb');
    });
}

abstract class API {

    static Identity = API.getUrlFactory('/api/Identity');   
    static Import = API.getUrlFactory('/api/Import');
    static Devices = API.getUrlFactory('/api/Devices');
    static SecurityQuestions = API.getUrlFactory('/api/SecurityQuestions');
    static SecurityCheck = API.getUrlFactory('/api/SecurityChecks');

    private static getUrlFactory(base: string) {
        return function (sufffix: string | number = undefined) {
            return sufffix ? `${base}/${sufffix}` : base;
        }
    }
}