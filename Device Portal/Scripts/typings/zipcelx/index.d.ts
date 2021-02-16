declare function zipcelx(config: Zipcelx.Config);

declare namespace Zipcelx {
    type Cell = {
        value: string | number,
        type?: 'string' | 'number';
    };

    type Row = Array<Cell>;

    type Sheet = {
        data: Array<Row>
    };

    export type Config = {
        filename: string,
        sheet: Sheet
    };
}
