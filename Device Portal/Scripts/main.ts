namespace DP {
    export let isPageSwap = true;

    // Note: must match pages array below
    export enum Page {
        Device,
        SecurityCheck,
        Users,
        Faculty,
        Institute,
        Requests,
        Admin,
        Home, // Make sure this is last since it's pattern matches all paths
    }

    export let pages = [
        '/device',
        '/securitycheck',
        '/users',
        '/faculty',
        '/institute',
        '/requests',
        '/admin',
        '/',
    ];

    export interface DeviceTableSettings {
        columns: { label: string, active: boolean }[];
    }

    export let iPage = Page.Home;
    export let confirmModal = new ConfirmModal();
    export let contextModal = new ContextualModal();
    export let noteModal = new NoteModal();
    export let deviceModal = new DeviceModal();

    class ActiveUser {
        user_name: string;
        first_name: string;
        last_name: string;    
        can_secure: boolean;
        can_approve: boolean;
        can_manage: boolean;
        can_admin: boolean;
        impersonating: boolean;

        page_access: { [key: number]: boolean } = {};

        constructor(claims: { type: string, value: string; }[]) {
            for (let claim of claims) {
                switch (claim.type) {
                    case 'uids': this.user_name = claim.value; break;
                    case 'given_name': this.first_name = claim.value; break;
                    case 'family_name': this.last_name = claim.value; break;
                    case 'https://secure.datanose.nl/claims/permission':
                        if (claim.value == "CanSecure") { this.can_secure = true; }
                        if (claim.value == "CanApprove") { this.can_approve = true; }
                        if (claim.value == "CanManage") { this.can_manage = true; }
                        if (claim.value == "CanAdmin") { this.can_admin = true; }
                        break;
                    case 'https://secure.datanose.nl/claims/impersonation':
                        if (claim.value == "true") { this.impersonating = true; }
                        break;
                }
            }

            this.page_access[Page.Home] = true;
            this.page_access[Page.Device] = true;

            // Secure & Approve
            this.page_access[Page.Requests] = this.can_approve;
            this.page_access[Page.SecurityCheck] = this.can_approve || this.can_secure;

            // Manage
            this.page_access[Page.Users] = this.can_manage;
            this.page_access[Page.Faculty] = this.can_manage;
            this.page_access[Page.Institute] = this.can_manage;

            // Admin
            this.page_access[Page.Admin] = this.can_admin;
        }
    }
    let user: ActiveUser;
    let init = false;
    ks.run(function () {
        if (!init) {
            init = true;
            GET(API.Identity()).done((claims: { type: string, value: string; }[]) => {
                if (claims && claims.length) {
                    user = new ActiveUser(claims);
                    ks.refresh();
                } else if (window.location.href.indexOf('enter') == -1) {
                    // Trigger login screen if front-end loaded from cache
                    window.location.href = "/api/identity/enter";
                }
            });
        }
        if (!user) { return; }

        // Global modals, run before any pages
        deviceModal.run();
        noteModal.run();
        confirmModal.run();
        contextModal.run();

        {
            let settings = {
                columns: [
                    { label: 'Name', active: true },
                    { label: 'Device ID', active: true },
                    { label: 'Serial number', active: true },
                    { label: 'Type', active: true },
                    { label: 'Category', active: true },
                    { label: 'OS', active: true },
                    { label: 'Cost centre', active: true },
                    { label: 'Building', active: true },
                    { label: 'Room', active: true },
                    { label: 'Outlet', active: true },
                    { label: 'Labnet', active: false },
                    { label: 'Ipv4', active: false },
                    { label: 'Ipv6', active: false },
                ]
            };
            let stored = JSON.parse(window.localStorage.getItem('device_table_cols'));
            if (stored) {
                for (let i = 0; i < settings.columns.length; ++i) {
                    let col = settings.columns[i];
                    let colStored = stored.columns.find(c => c.label === col.label);
                    if (colStored) { col.active = colStored.active; }
                }
            }

            ks.local_persist('####device_table_cols', settings);
            ks.local_persist('####table_settings_workaround', { counter: 0 });
        }

        let iPagePrev = iPage;
        let pathname = window.location.pathname.toLowerCase();
        let parameters = '';
        for (let i = 0; i < pages.length; ++i) {
            if (pathname.indexOf(pages[i]) === 0) {
                iPage = i;
                parameters = decodeURIComponent(window.location.pathname.substring(pages[i].length + 1));
                break;
            }
        }
        if (!user.page_access[iPage]) {
            ks.navigate_to('Home', '/');
            return;
        }
        isPageSwap = isPageSwap || iPage !== iPagePrev;

        // Setup request count update interval
        let requestCount = ks.local_persist('request count', { count: 0 });
        if (user.can_approve) {
            if (isPageSwap) {
                // If we swap to the Requests page we want to immediately update this number, interval might be far out
                GET(API.SecurityCheck('Submitted/Count')).done(c => {
                    requestCount.count = c;
                    ks.refresh();
                });
            }
            ks.set_interval('fetch request count', 60000, function () {
                GET(API.SecurityCheck('Submitted/Count')).done(c => {
                    requestCount.count = c;
                    ks.refresh();
                });
            }, !isPageSwap);
        }

        ks.nav_bar('top bar', 'navbar-expand navbar-light bg-white shadow-sm', function () {
            ks.set_next_item_class_name('navbar-nav');
            ks.group('container', 'container-fluid px-lg-4 d-flex justify-content-between', function () {
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

                    if (user.page_access[Page.Faculty]) {
                        ks.nav_item('Institutes', iPage === Page.Faculty, pages[Page.Faculty]);
                        ks.is_item_clicked(function () {
                            ks.navigate_to('Institutes', pages[Page.Faculty]);
                            return false;
                        });
                    }

                    if (user.page_access[Page.Users]) {
                        ks.nav_item('Users', iPage === Page.Users, pages[Page.Users]);
                        ks.is_item_clicked(function () {
                            ks.navigate_to('Users', pages[Page.Users]);
                            return false;
                        });
                    }

                    if (user.page_access[Page.Requests]) {
                        ks.nav_item('requests', iPage === Page.Requests, pages[Page.Requests], function () {
                            ks.text('Requests', 'd-inline mr-1');
                            if (requestCount.count) {
                                ks.text(requestCount.count.toString(), 'badge badge-primary badge-pill');
                            }
                        });
                        ks.is_item_clicked(function () {
                            ks.navigate_to('Approval requests', pages[Page.Requests]);
                            return false;
                        });
                    }

                    if (user.page_access[Page.Admin]) {
                        ks.nav_item('Admin', iPage === Page.Admin, pages[Page.Admin]);
                        ks.is_item_clicked(function () {
                            ks.navigate_to('Admin', pages[Page.Admin]);
                            return false;
                        });
                    }
                });

                ks.text(user.first_name.substr(0, 1) + '. ' + user.last_name, 'ml-auto');
                ks.unordered_list('right', 'navbar-nav', function () {
                    if (user.impersonating) {
                        ks.nav_item('End impersonation', false, '');
                        ks.is_item_clicked(function () {
                            GET(API.Identity('impersonate/end')).always(function () {
                                window.location.reload();
                            });
                            return false;
                        });
                    } else {
                        ks.nav_item('Logout', false, '/logout');
                        ks.is_item_clicked(function () {
                            GET(API.Identity('exit')).always(function () {
                                window.location.reload();
                                // window.location.replace("https://login.uva.nl/adfs/ls/?wa=wsignout1.0");
                            });
                            return false;
                        });
                    }
                });
            });
        });

        ks.group(pages[iPage], 'container-fluid px-lg-5 my-3', function () {
            switch (iPage) {
                case Page.Device:
                    page_device.call(this, parameters);
                    break;
                case Page.SecurityCheck:
                    page_security_check.call(this, parameters);
                    break;
                case Page.Users:
                    page_users.call(this, parameters);
                    break;
                case Page.Faculty:
                    page_faculty.call(this, parameters);
                    break;
                case Page.Institute:
                    page_institute.call(this, parameters);
                    break;
                case Page.Requests:
                    page_requests.call(this, parameters);
                    break;
                case Page.Admin:
                    page_admin.call(this);
                    break;
                default:
                    page_home.call(this);
                    break;
            }
        });

        isPageSwap = false;
    });

    function page_home() {
        let state = ks.local_persist('page_home', {
            devices: <Device[]>null,
        });
        if (isPageSwap) {
            GET_ONCE('devices', API.Devices('me')).done((result: Device[]) => {
                state.devices = result;
                ks.refresh();
            });
        }
        if (!state.devices) { return; }  // wait for devices

        ks.h5('My devices', 'mb-2');
        ks.row('devices', function () {
            ks.set_next_item_class_name('mb-3');
            ks.column('devices', 12, function () {
                ks.set_next_item_class_name('bg-white border');
                // TODO: remove on fix
                let workaround: { counter: number } = ks.local_persist('####table_settings_workaround');
                ks.table('devices##' + workaround.counter, function () {
                    let flags = DTF.EditDevice;
                    if (user.can_secure) { flags |= DTF.CanSecure; }

                    device_table_head(flags);

                    ks.table_body(function () {
                        for (let i = 0; i < state.devices.length; ++i) {
                            if (!state.devices[i].disowned) {
                                device_row(state.devices[i], flags, '');
                            }
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

    export function deviceIcon(type: DeviceType): string {
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

    // Device Table Flags
    export enum DTF {
        CanSecure = 1 << 0,
        EditDevice = 1 << 1,
        EditNote = 1 << 2,
        ShowSharedColumn = 1 << 3,
    }

    // EditNote flag has no effect on headers
    export function device_table_head(flags: DTF) {
        ks.table_head(function () {
            let settings: DeviceTableSettings = ks.local_persist('####device_table_cols');
            ks.table_row(function () {
                for (let i = 0; i < settings.columns.length; ++i) {
                    let c = settings.columns[i];
                    if (c.active) { ks.table_cell(c.label); }
                }

                ks.table_cell('Status').style.width = '1%';
                if (flags & DTF.CanSecure) { ks.table_cell('').style.width = '1%'; }
                if (flags & DTF.EditDevice) { ks.table_cell('').style.width = '1%'; }
                ks.table_cell(function () {
                    ks.group('dropdown', 'dropdown', function () {
                        ks.set_next_item_class_name('dropdown-toggle btn-sm');
                        ks.button('##btn', ks.no_op, 'outline-secondary').setAttribute('data-toggle', 'dropdown');
                        ks.group('menu', 'dropdown-menu dropdown-menu-right', function () {
                            for (let i = 0; i < settings.columns.length; ++i) {
                                let c = settings.columns[i];
                                dropdown_item(c.label, c.active, function () {
                                    c.active = !c.active;
                                    // TODO: remove when fixed
                                    (<any>ks.local_persist('####table_settings_workaround')).counter++;
                                    window.localStorage.setItem('device_table_cols', JSON.stringify(settings));
                                    ks.refresh();
                                });
                            }
                        });
                    });
                }).style.width = '1%';
            });
        });
    }

    export function device_row(d: Device, flags: DTF, user: string) {
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

        // Note: adding/removing cells here requires the same change in table headers wherever this method is called
        // Usually this is covered by the device_table_head(), but e.g. the institute page has custom headers.
        ks.table_row(function () {
            let settings: DeviceTableSettings = ks.local_persist('####device_table_cols');
            let cols = settings.columns;

            if (flags & DTF.ShowSharedColumn) {
                if (d.shared) {
                    ks.table_cell(function () {
                        ks.icon('fa fa-users');
                    }).title = 'Multiple users';
                } else { ks.table_cell(''); }
            }
            if (user) { ks.table_cell(user); }
            if (cols[0].active) { ks.table_cell(d.name); }
            if (cols[1].active) { ks.table_cell(d.deviceId); }
            if (cols[2].active) { ks.table_cell(d.serialNumber); }
            if (cols[3].active) {
                ks.table_cell(function () {
                    let i = ks.icon(icon);
                    i.style.width = '18px';
                    i.style.fontSize = iconSize;
                    ks.text(' ' + (deviceTypes[d.type] || ""), 'd-inline ml-1');
                });
            }
            if (cols[4].active) { ks.table_cell(deviceCategories[d.category]); }
            if (cols[5].active) { ks.table_cell(osNames[d.os_type]); }
            if (cols[6].active) { ks.table_cell(d.costCentre); }
            if (cols[7].active) { ks.table_cell(d.itracsBuilding); }
            if (cols[8].active) { ks.table_cell(d.itracsRoom); }
            if (cols[9].active) { ks.table_cell(d.itracsOutlet); }
            if (cols[10].active) { ks.table_cell(d.labnetId ? ('Labnet-' + d.labnetId) : ''); }
            if (cols[11].active) { ks.table_cell(d.ipv4); }
            if (cols[12].active) { ks.table_cell(d.ipv6); }
            ks.table_cell(function () {
                if (!(d.category & (DeviceCategory.ManagedSpecial | DeviceCategory.ManagedStandard))) {
                    ks.text(statusNames[d.status], 'badge badge-' + statusColors[d.status]);
                }
            });

            if (flags & DTF.CanSecure) {
                ks.table_cell(function () {
                    if (d.status != DeviceStatus.Approved) {
                        ks.set_next_item_class_name('text-nowrap');
                        ks.anchor('Security check', pages[Page.SecurityCheck] + '/' + d.id);
                        ks.is_item_clicked(function () {
                            ks.navigate_to('Security check', pages[Page.SecurityCheck] + '/' + d.id);
                            return false;
                        });
                    }
                });
            }
            if (flags & DTF.EditDevice) {
                ks.set_next_item_class_name('cursor-pointer');
                ks.table_cell(function () {
                    ks.icon('fa fa-pencil');
                });
                ks.is_item_clicked(function () {
                    ks.navigate_to('Device', pages[Page.Device] + '/' + d.id);
                    return false;
                });
            }

            let showIcon = (flags & DTF.EditNote) || d.notes;
            if (showIcon) { ks.set_next_item_class_name('cursor-pointer'); }
            ks.table_cell(function () {
                if (showIcon) {
                    ks.icon(d.notes ? 'fa fa-sticky-note text-warning' : 'fa fa-sticky-note-o text-muted');
                }
            });
            if (showIcon && !(flags & DTF.EditNote)) {
                ks.is_item_clicked(function () {
                    noteModal.show(d.notes);
                    return false;
                });
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

        ks.h5('Impersonate', 'mb-2 mt-2');
        ks.form('impersonate', '', true, function () {
            let userName;
            ks.input_text('UserName', '', 'UserName', function (val) {
                userName = val;
            });
            ks.button('Impersonate', function () {
                if (userName) {
                    GET(API.Identity('impersonate/' + userName)).then(function () {
                        ks.navigate_to('Home', pages[Page.Home]);
                        window.location.reload();
                    });
                }
            });
        });

        ks.h5('Tools', 'mb-2 mt-2');
        ks.button('sync-devices', function () {
            GET(API.Intune('managedDevices/sync')).done(function () {
                contextModal.showSuccess('Synchronization complete.');
            });
        });
        ks.set_next_item_class_name('ml-2');
        ks.button('sync-users', function () {
            GET(API.Intune('users/sync')).done(function () {
                contextModal.showSuccess('Synchronization complete.');
            });
        });        
        ks.set_next_item_class_name('ml-2');
        ks.button('sync-rights', function () {
            GET(API.Admin('update/rights')).done(function () {
                contextModal.showSuccess('Rights synchronization with DN complete.');
            });
        });
        ks.set_next_item_class_name('ml-2');
        ks.button('notify-approvers', function () {
            GET(API.Admin('notify/approvers')).done(function () {
                contextModal.showSuccess('Approvers notified of submitted checks.');
            });
        });
    }

    export function header_breadcrumbs(items: string[], proc: (i: number) => void) {
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

    export abstract class API {

        static Admin = API.getUrlFactory('/api/Admin');
        static Identity = API.getUrlFactory('/api/Identity');
        static Intune = API.getUrlFactory('/api/Intune');
        static Import = API.getUrlFactory('/api/Import');
        static Devices = API.getUrlFactory('/api/Devices');
        static Faculties = API.getUrlFactory('/api/Faculties');
        static Institutes = API.getUrlFactory('/api/Department');
        static SecurityQuestions = API.getUrlFactory('/api/SecurityQuestions');
        static SecurityCheck = API.getUrlFactory('/api/SecurityChecks');
        static Users = API.getUrlFactory('/api/Users');

        private static getUrlFactory(base: string) {
            return function (sufffix: string | number = undefined) {
                return sufffix ? `${base}/${sufffix}` : base;
            }
        }
    }

    export function paginator_range(id: string, total: number): { i_start: number, i_end: number, reset: () => void } {
        let p = ks.local_persist(id, { page: 0, pages: 1, count: 20 });
        return {
            i_start: p.count * p.page,
            i_end: Math.min(total, p.count * (p.page + 1)),
            reset: () => {
                p.page = 0;
                p.pages = 1;
            },
        };
    }

    export function paginator(id: string, total: number, proc_change: (i_page: number, count: number) => void) {
        let p = ks.local_persist(id, { page: 0, pages: 1, count: 20 });
        let range = paginator_range(id, total);
        p.pages = Math.ceil(total / p.count);

        ks.group(id, 'd-flex align-items-center', function () {
            let pagination = this;
            ks.group('records', 'flex-grow-1', function () {
                if (!total) { return; }
                if (p.pages === 1) { ks.text('Showing ' + total + ' results.'); }
                else { ks.text('Showing ' + (range.i_start + 1) + ' to ' + range.i_end + ' of ' + total); }
            });
            ks.form('pagination-form', '', true, function () {
                if (p.pages > 1) {
                    ks.text('Page', 'mr-1');
                    ks.group('page', 'form-group mr-3', function () {
                        ks.set_next_item_class_name('custom-select-sm');
                        ks.combo('page', function () {
                            for (let i = 0; i < p.pages; ++i) {
                                ks.selectable((i + 1).toString(), p.page == i);
                                ks.is_item_clicked(function () {
                                    p.page = i;
                                    ks.refresh(pagination);
                                    proc_change(p.page, p.count);
                                });
                            }
                        });
                    });
                }

                ks.text('Show', 'mr-1');
                ks.group('count', 'form-group', function () {
                    ks.set_next_item_class_name('custom-select-sm');
                    ks.combo('page-count', function () {
                        for (let option of [10, 20, 50, 100, 150, 200]) {
                            ks.selectable(option.toString(), p.count === option);
                            ks.is_item_clicked(function () {
                                let i_start = p.count * p.page;
                                p.count = option;
                                p.pages = Math.ceil(total / p.count);
                                p.page = Math.floor(p.pages * i_start / (total - 1));
                                ks.refresh(pagination);
                                proc_change(p.page, p.count);
                            });
                        }
                    });
                });
            });
        });
    }

    export function dropdown_item(id_label, active: boolean, proc: Function) {
        let parent = ks.get_current_parent();
        ks.set_next_item_class_name('dropdown-item d-flex align-items-center');
        ks.button_container(id_label, function () {
            ks.icon(active ? 'fa fa-check mr-2' : 'mr-2').style.width = '16px';
            ks.text(ks.label_extract(id_label));
        });
        ks.is_item_clicked(function (_, ev) {
            ev.preventDefault();
            ev.stopPropagation();
            proc.call(parent, _, ev);
        });
    }

    let collator = new Intl.Collator('en', { sensitivity: 'base' });
    export function sort_string(a: string, b: string) {
        return collator.compare(a, b);
    }

    export function sort_bool(a: boolean, b: boolean) {
        return a && !b ? -1 : (b && !a ? 1 : 0);
    }
}