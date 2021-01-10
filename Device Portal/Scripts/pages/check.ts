class SecurityCheck {
    id: number;
    userName: string;
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

                    state.security_check.questions = state.questions.map(q => {
                        return {
                            answer: undefined,
                            explanation: '',
                            question: q.text,
                            recommendations: q.recommendations,
                            mask: q.mask,
                        };
                    });
                    ks.
                        refresh();
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
        for (let i = 0; i < state.security_check.questions.length; ++i) {
            let q = state.security_check.questions[i];
            if (!(q.mask & state.device.type)) { continue; }

            ks.push_id(i.toString());

            if (!q.recommendations?.length) { ks.set_next_item_class_name('mb-1'); }
            ks.text(q.question, 'font-weight-bold');

            if (q.recommendations?.length) {
                let g = ks.group('recommendation', 'text-muted mb-1', ks.no_op);
                if (!g.children.length) {
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

        ks.button('Submit', ks.no_op);

        if (ks.current_form_submitted()) {
            if (this.getElementsByClassName('is-invalid').length) {
                ks.cancel_current_form_submission();
            } else {
                if (state.security_check.id) {
                    PUT_JSON(API.SecurityCheck(state.security_check.id), state.security_check).then(function () {
                        ks.navigate_to('Home', '/');
                        state.device.status = DeviceStatus.Submitted;
                    }, function (error) { contextModal.showWarning(error.responseText); });
                } else {
                    POST_JSON(API.SecurityCheck(), state.security_check).then(function () {
                        ks.navigate_to('Home', '/');
                        state.device.status = DeviceStatus.Submitted;
                    }, function (error) { contextModal.showWarning(error.responseText); });
                }
            }
        }
    });
}