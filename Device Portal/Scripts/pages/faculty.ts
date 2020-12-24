let faculties = [
    { name: 'Informatics', numAuthorized: 123, numCompleted: 100, numDevices: 321 },
    { name: 'Mathematics', numAuthorized: 123, numCompleted: 100, numDevices: 321 },
];

function page_faculty(parameters: string) {
    header_breadcrumbs(['Faculty'], ks.no_op);

    ks.row('faculties', function () {
        ks.set_next_item_class_name('mb-3');
        ks.column('faculties', 12, function () {
            for (let i = 0; i < faculties.length; ++i) {
                let f = faculties[i];

                ks.group(f.name, 'card mb-3',function () {
                    ks.group('body', 'card-body', function () {
                        ks.h5(f.name, 'card-title');
                        ks.text('Completed: ' + f.numCompleted, 'd-inline mr-3');
                        ks.text('Authorized: ' + f.numAuthorized, 'd-inline mr-3');
                        ks.text('Devices: ' + f.numDevices, 'd-inline');
                    });
                });
            }
        });
    });
}