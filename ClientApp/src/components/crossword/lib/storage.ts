declare global {
  interface Window {
    getItem(key: string): any;
    setItem(key: string, value: any): boolean | void;
    removeItem(key: string): void;
    [index: string]: any;
  }
}

export interface IStorage {
  storage: Window;
  available: boolean | undefined;
  isAvailable: () => boolean;
  getItem: (key: string) => any;
  setItem: (key: string, value: any, options: any) => boolean | void;
  removeItem: (key: string) => void;
  setIfNotExists: (key: string, value: any, options: any) => boolean | void;
  getRaw: (key: string) => string | undefined;
}

class Storage implements IStorage {
  public storage: Window;
  public available: boolean | undefined;

  constructor(type: string) {
    this.storage = window[type];
    this.available = this.isAvailable();
  }

  isAvailable(): boolean {
    const key = 'local-storage-module-test';

    if (this.available !== undefined) {
      return this.available;
    }

    try {
      // to fully test, need to set item
      // http://stackoverflow.com/questions/9077101/iphone-localstorage-quota-exceeded-err-issue#answer-12976988
      this.storage.setItem(key, 'graun');
      this.storage.removeItem(key);
      this.available = true;
    } catch (err) {
      this.available = false;
    }

    return this.available;
  }

  getItem(key: string): any {
    if (!this.available) {
      return null;
    }

    let data;

    // try and parse the data
    try {
      const value = this.getRaw(key);

      if (value === null || value === undefined) {
        return null;
      }

      data = JSON.parse(value);

      if (data === null) {
        return null;
      }
    } catch (e) {
      this.removeItem(key);
      return null;
    }

    // has it expired?
    if (data.expires && new Date() > new Date(data.expires)) {
      this.removeItem(key);
      return null;
    }

    return data.value;
  }

  setItem(key: string, value: any, options: any = {}): boolean | void {
    if (!this.available) {
      return undefined;
    }

    return this.storage.setItem(
      key,
      JSON.stringify({
        value,
        expires: options.expires
      })
    );
  }

  setIfNotExists(key: string, value: any, options: any = {}): boolean | void {
    if (!this.available) {
      return undefined;
    }

    if (this.storage.getItem(key) !== null) {
      return undefined;
    }

    return this.storage.setItem(
      key,
      JSON.stringify({
        value,
        expires: options.expires
      })
    );
  }

  getRaw(key: string): string | undefined {
    if (this.available) {
      return this.storage.getItem(key);
    }
    return undefined;
  }

  removeItem(key: string): void {
    if (this.available) {
      return this.storage.removeItem(key);
    }
    return undefined;
  }
}

export const local = new Storage('localStorage');
export const session = new Storage('sessionStorage');
