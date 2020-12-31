class SecurityCheck {
    deviceId: number;
    submissionDate: string;
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
let questions: SecurityQuestion[] = [];

function page_security_check(parameters: string) {
    let state = ks.local_persist('page_security_check', {
        security_check: <SecurityCheck>null,
        device: <Device>null,
    });
    if (isPageSwap) {
        state.security_check = null;
        GET_ONCE('questions', API.SecurityQuestions()).done((result: SecurityQuestion[]) => {
            questions = result;
            state.security_check = new SecurityCheck();                
            state.security_check.questions = questions.map(q => {
                return {
                    answer: undefined,
                    explanation: '',
                    question: q.text,
                    mask: q.mask,
                };
            });
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
                GET_ONCE('device', API.Devices(deviceId)).done(d => {
                    state.device = d;
                }),
                GET_ONCE('security_check', API.Devices(`${deviceId}/SecurityCheck`)).done(c => {
                    state.security_check = c;
                })).always(function () {
                    if (state.device && state.security_check) { state.security_check.deviceId = state.device.id; }
                    ks.refresh();
                });
                
            return; // wait for get device & security check
        }
    }

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
                POST_JSON(API.SecurityCheck(), state.security_check).then(function () {
                    ks.navigate_to('Home', '/');
                    state.device.status = DeviceStatus.Submitted;
                }, function (error) { contextModal.showWarning(error.responseText); });
            }
        }
    });
}