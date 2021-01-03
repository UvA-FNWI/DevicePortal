﻿class SecurityCheck {
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
        mask: DeviceType;
    }[] = [];
}

class SecurityQuestion {
    text: string;
    mask: DeviceType;
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
                GET_ONCE('get_device', API.Devices(deviceId)).then(d => {
                    state.device = d;
                }, fail => {
                    if (fail.status === 404) { contextModal.showWarning("Device not found"); }
                    ks.navigate_to('Users', pages[Page.Home])
                }),
                GET_ONCE('get_security_check', API.SecurityCheck(`Device/${deviceId}`)).then((c: SecurityCheck) => {
                    // Allow user to edit existing submission, otherwise start new request
                    if (c.status === DeviceStatus.Submitted) {
                        state.security_check = c;
                    } else { security_check_not_found(); }
                }, security_check_not_found)).always(function () {
                    if (state.device && state.security_check) { state.security_check.deviceId = state.device.id; }
                    ks.refresh();
                });

            function security_check_not_found() {
                state.security_check = new SecurityCheck();
                state.security_check.questions = state.questions.map(q => {
                    return {
                        answer: undefined,
                        explanation: '',
                        question: q.text,
                        mask: q.mask,
                    };
                });
            }

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

            ks.text(q.question, 'font-weight-bold mb-1');
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