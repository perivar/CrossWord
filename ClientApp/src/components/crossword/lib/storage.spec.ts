import MockDate from 'mockdate';
import { local, session, IStorage } from './storage';

interface StorageIO {
  key: string;
  data: {};
  expected: {};
  options: { expires?: Date };
  find?: (key: string) => string;
}

const IO: StorageIO[] = [
  {
    key: 'test-1',
    data: 'string',
    expected: '{"value":"string"}',
    options: {}
  },

  {
    key: 'test-2',
    data: { foo: 'bar' },
    expected: '{"value":{"foo":"bar"}}',
    options: {}
  },

  {
    key: 'test-3',
    data: [true, 2, 'bar'],
    expected: '{"value":[true,2,"bar"]}',
    options: {}
  },

  {
    key: 'test-4',
    data: 'test-4',
    options: {
      expires: new Date('2100-01-01')
    },
    expected: '{"value":"test-4","expires":"2100-01-01T00:00:00.000Z"}'
  },

  {
    key: 'test-5',
    data: false,
    expected: '{"value":false}',
    options: {}
  }
];

// https://gist.github.com/mayank23/7b994385eb030f1efb7075c4f1f6ac4c
const testStorage = (storageName: string, fn: IStorage) => {
  const engine = fn;

  // jsdom doesn't support localStorage/ sessionStorage
  const property = `${storageName}Storage`;
  const { [property]: originalProperty } = window;
  delete window[property];
  beforeAll(() => {
    Object.defineProperty(window, property, {
      configurable: true,
      writable: true,
      value: {
        getItem: jest.fn((key: string): {} | undefined => {
          const item = IO.find((io: StorageIO) => io.key === key);
          return item && item.expected;
        }),
        setItem: jest.fn(),
        removeItem: jest.fn()
      }
    });

    engine.storage = window[property];
  });

  beforeEach(() => {
    engine.available = true;
    // jest.spyOn(engine.storage, 'setItem');
  });

  afterEach(() => {
    // engine.storage.setItem.mockRestore();
  });

  afterAll(() => {
    window[property] = originalProperty;
  });

  test(`${storageName} - isAvailable()`, () => {
    engine.available = undefined;
    expect(engine.isAvailable()).toBe(true);
    expect(engine.available).toBe(true);
    expect(engine.storage.setItem).toHaveBeenCalledWith('local-storage-module-test', 'graun');
  });

  test(`${storageName} - is(Not)Available()`, () => {
    const origSet = engine.storage.setItem;

    // not available, if setItem fails
    engine.available = undefined;
    engine.storage.setItem = () => {
      throw new Error('Problem!');
    };
    expect(engine.isAvailable()).toBe(false);

    engine.storage.setItem = origSet;
  });

  test(`${storageName} - isAvailable() cache`, () => {
    // not available, if setItem fails
    engine.available = false;
    expect(engine.isAvailable()).toBe(false);
  });

  test(`${storageName} - set()`, () => {
    IO.forEach(({ key, data, expected, options }) => {
      (engine.storage.setItem as jest.Mock).mockClear();
      engine.setItem(key, data, options);
      expect(engine.storage.setItem).toHaveBeenCalledWith(key, expected);
    });
  });

  test(`${storageName} - get()`, () => {
    IO.forEach(({ key, data }) => {
      expect(engine.getItem(key)).toEqual(data);
    });
  });

  test(`${storageName} - get() with expired item`, () => {
    IO.filter((item) => item.options && item.options.expires).forEach((expired) => {
      const { key } = expired;

      // set expired
      MockDate.set(new Date(2100, 1, 2, 0, 0, 0, 0));

      expect(engine.getItem(key)).toEqual(null);
      expect(engine.storage.removeItem).toHaveBeenCalledWith(key);
      (engine.storage.removeItem as jest.Mock).mockClear();

      // reset
      MockDate.reset();
    });
  });

  test(`${storageName} - get() with non-expired item`, () => {
    IO.filter((item) => item.options && item.options.expires).forEach((expired) => {
      const { key, data } = expired;

      // set non-expired
      MockDate.set(new Date(2099, 1, 1, 0, 0, 0, 0));

      expect(engine.getItem(key)).toEqual(data);
      expect(engine.storage.removeItem).not.toHaveBeenCalled();
      (engine.storage.removeItem as jest.Mock).mockClear();

      // reset
      MockDate.reset();
    });
  });

  test(`${storageName} - getRaw()`, () => {
    IO.forEach(({ key, expected }) => {
      expect(engine.getRaw(key)).toBe(expected);
    });
  });

  test(`${storageName} - remove()`, () => {
    IO.forEach(({ key }) => {
      (engine.storage.removeItem as jest.Mock).mockClear();
      engine.removeItem(key);
      expect(engine.storage.removeItem).toHaveBeenCalledWith(key);
    });
  });
};

describe('sessionStorage', () => {
  testStorage('session', session);
});

describe('localStorage', () => {
  testStorage('local', local);
});
