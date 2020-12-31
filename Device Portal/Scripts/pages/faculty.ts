let faculties = [
    { name: 'Informatics', numAuthorized: 123, numCompleted: 100, numDevices: 321 },
    { name: 'Mathematics', numAuthorized: 123, numCompleted: 100, numDevices: 321 },
];

function page_faculty(parameters: string) {
    header_breadcrumbs(['Faculty'], ks.no_op);

    ks.set_next_item_class_name('mx-n2');
    ks.row('faculties', function () {
        for (let i = 0; i < faculties.length; ++i) {
            let f = faculties[i];

            ks.column(i.toString(), '12 col-md-6 px-2', function () {
                ks.group(f.name, 'card mb-3', function () {
                    ks.group('body', 'card-body', function () {
                        ks.h5(f.name, 'card-title');

                        ks.group('devices', 'mb-2', function () {
                            ks.icon('fa fa-microchip mr-1 d-inline-block').style.width = '16px';
                            ks.text('Registered devices: ', 'd-inline');
                            ks.text('' + f.numDevices + ' ', 'd-inline');
                        });

                        ks.group('completed', 'mb-1', function () {
                            ks.icon('fa fa-list-ol d-inline');
                            ks.text('Users completed their security checks:', 'ml-1 d-inline');
                        });
                        ks.progress_bar('bar', f.numCompleted + ' / ' + f.numAuthorized, f.numCompleted, f.numAuthorized, 'bg-info');
                    });
                });
            });
        }
    });
}