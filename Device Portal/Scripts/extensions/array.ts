interface IWherePredicate<T> {
    (p: T): boolean;
}
interface ISelectPredicate<T> {
    (p: T): any;
}

interface Array<T> {
    where(predicate: IWherePredicate<T>): T[];
    select(predicate: ISelectPredicate<T>): any[];
    selectMany(predicate: ISelectPredicate<T>): any[];
    first(predicate?: IWherePredicate<T>): T;
    last(): T;
    init(size: number, value: T): T[];
    groupBy(predicate: (p: T) => string | number): { key: string | number, values: T[] }[];
    findIndex(predicate: IWherePredicate<T>): number;
    distinct(isSorted: boolean): T[];
    distinctBy(selector: (t: T) => string | number): T[];
    contains(predicate: T): boolean;
    any(predicate: IWherePredicate<T>): boolean;
    all(predicate: IWherePredicate<T>): boolean;    
}
if (typeof Array.prototype.where !== 'function') {
    Array.prototype.where = function where<T>(predicate: IWherePredicate<T>): T[] {
        var result = [];
        for (var i = 0; i < this.length; ++i) {
            if (predicate(this[i])) { result.push(this[i]); }
        }
        return result;
    };
}
if (typeof Array.prototype.selectMany !== 'function') {
    Array.prototype.selectMany = function selectMany<T>(predicate: ISelectPredicate<T>): any[] {
        var result = [];
        for (var i = 0; i < this.length; ++i) {
            let array = predicate(this[i]);
            for (let k = 0; k < array.length; ++k) { result.push(array[k]); }
        }
        return result;
    };

}
if (typeof Array.prototype.last !== 'function') {
    Array.prototype.last = function last<T>(): T {
        return this[this.length - 1];
    };
}
if (typeof Array.prototype.init !== 'function') {
    Array.prototype.init = function init<T>(size: number, value: T) {
        this.length = size;
        for (let i = 0; i < size; ++i) { this[i] = value; }
        return this;
    }
}
if (typeof Array.prototype.groupBy !== 'function') {
    Array.prototype.groupBy = function groupBy<T>(predicate: (p: T) => string | number): { key: string | number, values: T[] }[] {
        const resultMap = this.grouped(predicate);
        const result = [];
        resultMap.forEach((values, key) => {
            result.push({ key: key, values: values });
        });
        return result;
    };
}
if (typeof Array.prototype.first !== 'function') {
    Array.prototype.first = function first<T>(predicate: IWherePredicate<T>) {
        if (predicate) {
            let length = this.length;
            for (let i = 0; i < length; ++i) {
                if (predicate(this[i])) { return this[i]; }
            }
            return null;
        }
        return this[0];
    }
}
if (typeof Array.prototype.findIndex !== 'function') {
    Array.prototype.findIndex = function findIndex<T>(predicate: IWherePredicate<T>) {
        let length = this.length;
        for (let i = 0; i < length; ++i) {
            if (predicate(this[i])) { return i; }
        }
        return -1;
    };
}
if (typeof Array.prototype.distinct !== 'function') {
    Array.prototype.distinct = function distinct<T>(isSorted: boolean): T[] {
        let result = [];

        if (isSorted) {
            let last;
            if (this.length) {
                result[0] = this[0];
                last = this[0];
            }
            for (let i = 1; i < this.length; ++i) {
                if (this[i] !== last) {
                    result.push(this[i]);
                    last = this[i];
                }
            }
        } else {
            console.warn("The array really should be sorted, don't want O(n^2) algorithms!");

            for (let i = 0; i < this.length; ++i) {
                let duplicate = false;
                for (let k = 0; k < this.length; ++k) {
                    if (i != k && this[i] === this[k]) {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate) { result.push(this[i]); }
            }
        }

        return result;
    };
}
if (typeof Array.prototype.distinctBy !== 'function') {
    Array.prototype.distinctBy = function distinctBy<T>(selector: (t: T) => string | number): T[] {
        let existing = {};
        let result: T[] = [];
        for (let i = 0; i < this.length; ++i) {
            let key: any = selector(this[i]);
            if (!(key in existing)) {
                existing[key] = true;
                result.push(this[i]);
            }
        }
        return result;
    };
}
if (typeof Array.prototype.contains !== 'function') {
    Array.prototype.contains = function contains<T>(value: T): boolean {
        for (let i = 0; i < this.length; ++i) {
            if (this[i] === value) { return true; }
        }
        return false;
    };
}
if (typeof Array.prototype.any !== 'function') {
    Array.prototype.any = function any<T>(predicate: IWherePredicate<T>): boolean {
        for (let i = 0; i < this.length; ++i) {
            if (predicate(this[i])) { return true; }
        }
        return false;
    };
}
if (typeof Array.prototype.all !== 'function') {
    Array.prototype.all = function all<T>(predicate: IWherePredicate<T>): boolean {
        for (let i = 0; i < this.length; ++i) {
            if (!predicate(this[i])) { return false; }
        }
        return true;
    };
}