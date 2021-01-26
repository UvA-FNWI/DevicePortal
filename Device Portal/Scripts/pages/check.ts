class SecurityCheck {
    id: number;
    userName: string;
    userDisplayName: string;
    deviceId: number;
    device: Device;
    submissionDate: string;
    status: DeviceStatus;
    statusEffectiveDate: string;
    questions: {
        question: string;
        answer: boolean;
        explanation: string;
        recommendations: SecurityRecommendation[];
        mask: DeviceType;
    }[] = [];
}

class SecurityQuestion {
    text: string;
    mask: DeviceType;
    recommendations: SecurityRecommendation[];
}

class SecurityRecommendation {
    order: number;
    content: string;
    os_type: OSType;
}

function page_security_check(parameters: string) {
    let state = ks.local_persist('page_security_check', {
        questions: <SecurityQuestion[]>[],
        security_check: <SecurityCheck>null,
        device: <Device>null,
        refresh_recommendations: true,
        loaded_os_type: <OSType>0,
    });
    if (isPageSwap) {
        state.security_check = null;
        GET_ONCE('questions', API.SecurityQuestions()).done((result: SecurityQuestion[]) => {
            for (let i = 0; i < result.length; ++i) {
                result[i].recommendations.sort((a, b) => a.order - b.order);
            }
            state.questions = result;
            ks.refresh();
        });

        state.device = null;
        return; // wait for get questions
    }

    if (parameters) {
        let parts = parameters.split('/');
        let deviceId = parseInt(parts[0]);
        if (!isNaN(deviceId) && (!state.device || state.device.id != deviceId)) {
            
            $.when(
                GET(API.Devices(deviceId)).then(d => {
                    state.device = d;
                    state.loaded_os_type = d.os_type;
                    if (state.device.os_version == null) { state.device.os_version = ''; }
                }, fail => {
                    if (fail.status === 404) { contextModal.showWarning("Device not found"); }
                    ks.navigate_to('Users', pages[Page.Home])
                }),
                GET(API.SecurityCheck(`Device/${deviceId}`)).then((c: SecurityCheck) => {
                    // Allow user to edit existing submission, otherwise start new request
                    state.security_check = c.status === DeviceStatus.Submitted ? c : new SecurityCheck();
                }, () => {
                    state.security_check = new SecurityCheck();
                })).always(function () {
                    if (state.device && state.security_check) { state.security_check.deviceId = state.device.id; }

                    // Recommendations are not save in submitted questions
                    if (state.security_check.id) {
                        if (state.security_check.questions.length === state.questions.length) {
                            for (let i = 0; i < state.questions.length; ++i) {
                                state.security_check.questions[i].recommendations = state.questions[i].recommendations;
                            }
                        }
                    } else {
                        state.security_check.questions = state.questions.map(q => {
                            return {
                                answer: undefined,
                                explanation: '',
                                question: q.text,
                                recommendations: q.recommendations,
                                mask: q.mask,
                            };
                        });
                    }                    
                    ks.refresh();
                });

            return; // wait for get device & security check
        }
    } else { state.device = null; }

    if (!state.device) {
        ks.navigate_to('Home', '/');
        return;
    }

    header_breadcrumbs(['Security check'], ks.no_op);
    ks.group('sub header', 'mt-n3 mb-3 text-muted', function () {
        ks.icon(deviceIcon(state.device.type) + ' d-inline');
        ks.text(state.device.name, 'd-inline ml-1');
    });

    ks.form('security', '', false, function () {
        ks.set_next_item_class_name('mb-3');
        ks.row('os', function () {
            ks.group('type', 'col-auto mr-2', function () {
                ks.text('Operating system', 'mb-1');
                ks.set_next_input_validation(!!state.device.os_type, '', 'This is a required field.');
                ks.combo('Operating system', function () {
                    ks.selectable('##none', !state.device.os_type);
                    ks.is_item_clicked(function () {
                        state.device.os_type = 0;
                        ks.refresh();
                    });
                    for (let i = 0; i < osTypeCount; ++i) {
                        let type: OSType = 1 << i;
                        ks.selectable(osNames[type], state.device.os_type === type);
                        ks.is_item_clicked(function () {
                            state.device.os_type = type;
                            state.refresh_recommendations = true;
                            ks.refresh();
                        });
                    }
                }).disabled = !!(state.loaded_os_type & (OSType.Android | OSType.iOS));
            });

            ks.group('version', 'col col-md-6', function () {
                ks.text('Version', 'mb-1');
                ks.set_next_input_validation(!!state.device.os_version.length, '', 'This is a required field.');
                ks.input_text('version', state.device.os_version, 'Version', function (val) {
                    state.device.os_version = val;
                });
            });
        });

        for (let i = 0; i < state.security_check.questions.length; ++i) {
            let q = state.security_check.questions[i];
            if (!(q.mask & state.device.type)) { continue; }

            ks.push_id(i.toString());

            if (!q.recommendations?.length) { ks.set_next_item_class_name('mb-1'); }
            ks.text(q.question, 'font-weight-bold');

            if (q.recommendations?.length) {
                let g = ks.group('recommendation', 'text-muted mb-1', ks.no_op);
                if (state.refresh_recommendations) {
                    while (g.children.length) { g.removeChild(g.children[0]); }

                    let div = document.createElement("DIV");
                    let html = '';
                    for (let k = 0; k < q.recommendations.length; ++k) {
                        if (state.device.os_type & q.recommendations[k].os_type) {
                            html += q.recommendations[k].content;
                        }
                    }
                    div.innerHTML = html;
                    g.appendChild(div);
                    ks.mark_persist(div);
                }
            }

            ks.group('radios', 'mb-3', function () {
                ks.set_next_item_class_name('custom-control-inline');
                ks.radio_button('Yes', q.answer === true, function () {
                    q.answer = true;
                    ks.refresh();
                });

                ks.set_next_input_validation(q.answer !== undefined, '', 'Must select a value');
                ks.set_next_item_class_name('custom-control-inline');
                ks.radio_button('No', q.answer === false, function () {
                    q.answer = false;
                    ks.refresh();
                });
            });

            if (q.answer === false) {
                ks.set_next_input_validation(!!q.explanation, '', 'Please provide a clarification');
                ks.input_text_area('explanation', q.explanation, 'Please clarify', function (str) {
                    q.explanation = str;
                    ks.refresh(this);
                });
            }

            ks.pop_id();
        }
        state.refresh_recommendations = false;

        ks.button('Submit', ks.no_op);

        if (ks.current_form_submitted()) {
            if (this.getElementsByClassName('is-invalid').length) {
                ks.cancel_current_form_submission();
            } else {
                let xhrDevice = PUT_JSON(API.Devices(state.device.id), state.device);

                let xhrCheck = state.security_check.id ?
                    PUT_JSON(API.SecurityCheck(state.security_check.id), state.security_check) :
                    POST_JSON(API.SecurityCheck(), state.security_check);

                $.when(xhrDevice, xhrCheck).done(function () {
                    ks.navigate_to('Home', '/');
                    state.device.status = DeviceStatus.Submitted;
                }).fail(function (error) {
                    contextModal.showWarning(error.responseText);
                });
            }
        }
    });
}