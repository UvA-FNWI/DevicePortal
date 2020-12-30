function GET(url: string) {
    return $.get(url);
}
function GET_ONCE(id: string, url: string): JQuery.jqXHR {
    let fetching = ks.local_persist(id, { b: false });
    if (!fetching.b) {
        fetching.b = true;
        return $.get(url).always(() => {
            fetching.b = false;
        });
    }
    return <any>$.Deferred().reject();
}
function POST_JSON<T>(url: string, entity: any, dataType?: string): JQuery.jqXHR {
    let data = {};
    for (let key in entity) {
        let prop = <any>entity[key];
        if (typeof prop !== 'function') {
            if (prop instanceof Date) { data[key] = (<Date>prop).toISOString(); }
            else { data[key] = prop; }
        }
    }
    return $.ajax({
        'type': 'POST',
        'url': url,
        'contentType': 'application/json',
        'data': JSON.stringify(data),
        'dataType': dataType || 'json',
    });
};
function POST_PRIMITIVE(url: string, value: string | number | boolean): JQuery.jqXHR {
    return $.ajax({
        'type': 'POST',
        'url': url,
        'contentType': 'application/json',
        'data': JSON.stringify(value),
        'dataType': 'json',
    });
};
function PUT_JSON(url: string, entity: any): JQuery.jqXHR {
    let data = {};
    for (let key in entity) {
        let prop = <any>entity[key];
        if (typeof prop !== 'function') {
            if (prop instanceof Date) { data[key] = (<Date>prop).toISOString(); }
            else { data[key] = prop; }
        }
    }
    return $.ajax({
        'type': 'PUT',
        'url': url,
        'contentType': 'application/json',
        'data': JSON.stringify(data),
        'dataType': 'json',
    });
};
function PUT_PRIMITIVE(url: string, value: string | number | boolean): JQuery.jqXHR {
    return $.ajax({
        'type': 'PUT',
        'url': url,
        'contentType': 'application/json',
        'data': JSON.stringify(value),
        'dataType': 'json',
    });
};