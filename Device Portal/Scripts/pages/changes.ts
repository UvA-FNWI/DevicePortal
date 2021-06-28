namespace DP {
    export function page_changes(parameters: string) {
        let state = ks.local_persist('page_changes', {
            devices: <Device[]>[],
        })

        header_breadcrumbs(['Changes'], function () {
            ks.navigate_to('Changes', pages[Page.Changes]);
        });

        if (isPageSwap) {
            GET_ONCE('fetch device changes', API.Devices('Changes')).done((result: Device[]) => {
                state.devices = result;
                for (let d of state.devices) {
                    d.nameLowerCase = d.name ? d.name.toLowerCase() : '';
                    d.deviceIdLowerCase = d.deviceId ? d.deviceId.toLowerCase() : '';
                    d.serialNumberLowerCase = d.serialNumber ? d.serialNumber.toLowerCase() : '';
                    if (d.user) { d.user.nameLowerCase = d.user.name ? d.user.name.toLowerCase() : ''; }
                    d.costCentreLowerCase = d.costCentre ? d.costCentre.toLowerCase() : '';
                    d.itracsBuildingLowerCase = d.itracsBuilding ? d.itracsBuilding.toLowerCase() : '';
                    d.itracsRoomLowerCase = d.itracsRoom ? d.itracsRoom.toLowerCase() : '';
                    d.itracsOutletLowerCase = d.itracsOutlet ? d.itracsOutlet.toLowerCase() : '';

                    Device.formatPurchaseDate(d);
                    Device.formatLastSeenDate(d);
                }
                state.devices.sort((a, b) => sort_string(a.name, b.name));
                ks.refresh();
            });
        }
        if (!state.devices) { return; } // wait for get questions

        ks.row('device changes', function () {
            ks.set_next_item_class_name('mb-3');
            ks.column('devices', 12, function () {
                let range = paginator_range('paginator', state.devices.length);

                // TODO: remove on fix
                let workaround: { counter: number } = ks.local_persist('####table_settings_workaround');
                ks.set_next_item_class_name('bg-white border');
                ks.table('devices##' + workaround.counter, function () {
                    device_table_head(0);
                    ks.table_body(function () {
                        let countdown = range.i_end - range.i_start;
                        for (let i = range.i_start; countdown > 0; ++i) {
                            ks.set_next_item_class_name('cursor-pointer');
                            device_row(state.devices[i], DTF.EditNote, '');
                            ks.is_item_clicked(function () {
                                deviceModal.show(state.devices[i]);
                            });

                            --countdown;
                        }
                    });
                });

                ks.set_next_item_class_name('ml-1');
                // NOTE: adjust length if we add search functionality
                paginator('paginator', state.devices.length, () => ks.refresh(this));
            });
        });
    }
}