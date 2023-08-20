module.exports = {
  parser: '@typescript-eslint/parser', // Specifies the ESLint parser
  extends: [
    'eslint:recommended',
    'plugin:react/recommended', // Uses the recommended rules from @eslint-plugin-react
    'plugin:jsx-a11y/recommended',
    'plugin:import/typescript', // extends the recommended import with typescript import/resolver support for .ts and .tsx
    'plugin:@typescript-eslint/recommended', // Uses the recommended rules from the @typescript-eslint/eslint-plugin
    'plugin:prettier/recommended', // Enables eslint-plugin-prettier and eslint-config-prettier. This will display prettier errors as ESLint errors. Make sure this is always the last configuration in the extends array.
    'prettier'
  ],
  parserOptions: {
    // ecmaVersion: 2018, // Allows for the parsing of modern ECMAScript features
    // sourceType: 'module', // Allows for the use of imports
    // ecmaFeatures: {
    //   jsx: true,
    // },
    // project: 'tsconfig.json',
    // tsconfigRootDir: '.'
  },
  env: {
    es6: true,
    browser: true,
    node: true,
    jest: true
  },
  plugins: ['@typescript-eslint', 'react', 'react-hooks', 'jsx-a11y', 'prettier', 'json', 'import'],
  rules: {
    // Place to specify ESLint rules. Can be used to overwrite rules specified from the extended configs
    // e.g. '@typescript-eslint/explicit-function-return-type': 'off',

    // https://gist.github.com/1natsu172/a65a4b45faed2bd3fa74b24163e4256e

    /**
     * @description rules of @typescript-eslint
     */
    '@typescript-eslint/prefer-interface': 'off', // also want to use 'type'
    '@typescript-eslint/explicit-function-return-type': 'off', // annoying to force return type
    '@typescript-eslint/indent': 'off',
    '@typescript-eslint/interface-name-prefix': 'off', // off = disagree with TS team on usefulness
    '@typescript-eslint/no-explicit-any': 'off', // off = I'm only allergic to implicit any
    '@typescript-eslint/explicit-member-accessibility': 'off', // ok with implicit public
    '@typescript-eslint/no-non-null-assertion': 'off',
    '@typescript-eslint/no-use-before-define': 'off',
    '@typescript-eslint/ban-ts-ignore': 'off',
    '@typescript-eslint/ban-types': 'off', // this was too strict for the hooks

    '@typescript-eslint/ban-ts-comment': ['warn', { 'ts-ignore': 'allow-with-description' }],
    '@typescript-eslint/no-empty-interface': 'warn',
    '@typescript-eslint/no-empty-function': 'warn',

    // allow underscore for unused vars
    '@typescript-eslint/no-unused-vars': [
      'warn',
      {
        argsIgnorePattern: '^_',
        varsIgnorePattern: '^_',
        caughtErrorsIgnorePattern: '^_'
      }
    ],

    // Note: you must disable the base rule as it can report incorrect errors
    '@typescript-eslint/no-shadow': [
      'warn',
      {
        ignoreFunctionTypeParameterNameValueShadow: true,
        ignoreTypeValueShadow: true
      }
    ],

    // '@typescript-eslint/camelcase': 'off', // deprectated use naming-convention instead
    '@typescript-eslint/naming-convention': [
      'error',
      {
        selector: 'variable',
        format: ['camelCase', 'UPPER_CASE', 'PascalCase']
      },
      {
        selector: 'variable',
        modifiers: ['destructured'],
        format: null
      }
    ],

    /**
     * @description rules of eslint-plugin-react
     */
    'react/jsx-filename-extension': [
      'warn',
      {
        // also want to use with '.tsx'
        extensions: ['.jsx', '.tsx']
      }
    ],
    'react/jsx-no-bind': [
      'error',
      {
        allowArrowFunctions: true,
        allowFunctions: true,
        allowBind: true
      }
    ],
    'react/no-typos': 'error',

    'react/jsx-props-no-spreading': 'off',
    'react/prop-types': 'off', // Is this incompatible with TS props type?
    'react/destructuring-assignment': 'off',
    'react/require-default-props': 'off',
    'react/jsx-no-useless-fragment': 'off',

    'react/jsx-no-duplicate-props': 'warn',
    'react/jsx-pascal-case': 'warn',
    'react/sort-default-props': 'warn',
    'react/no-find-dom-node': 'warn',
    'react/no-string-refs': 'warn',
    'react/no-unused-state': 'warn',
    'react/no-access-state-in-setstate': 'warn',
    'react/no-unsafe': 'warn',
    'react/no-unused-prop-types': 'warn',
    'react/no-array-index-key': 'warn',
    'react/static-property-placement': ['warn', 'static public field'],
    'react/prefer-stateless-function': ['warn', { ignorePureComponents: true }],

    /**
     * @description rules of eslint-plugin-react-hooks
     */
    'react-hooks/rules-of-hooks': 'error',
    'react-hooks/exhaustive-deps': 'warn',

    /**
     * @description rules of eslint
     */
    'spaced-comment': [
      'error',
      'always',
      {
        markers: ['/']
      }
    ],

    // Note: you must disable the base rule as it can report incorrect errors
    camelcase: 'off',
    indent: 'off',
    'no-shadow': 'off',
    'no-empty-function': 'off',
    'no-unused-vars': 'off',

    'no-underscore-dangle': 'off',
    'no-console': 'off',
    'no-nested-ternary': 'off',
    'no-return-assign': 'off',
    'no-use-before-define': 'off', // this was too strict for the hooks
    'lines-between-class-members': 'off',

    'default-param-last': 'warn',
    'prefer-const': 'warn',
    'prefer-destructuring': ['warn', { object: true, array: false }],
    'no-dupe-class-members': 'warn',
    'no-else-return': 'warn',
    'no-unneeded-ternary': 'warn',
    'no-restricted-syntax': 'warn',
    'no-cond-assign': 'warn',
    'vars-on-top': 'warn',
    'prefer-template': 'warn',
    'prefer-object-spread': 'warn',
    'no-param-reassign': ['off', { props: true, ignorePropertyModificationsFor: ['draft'] }], // if using warn we support immer, currently off
    'no-plusplus': ['warn', { allowForLoopAfterthoughts: true }],
    'no-unused-expressions': 'warn',
    'object-shorthand': 'warn',
    'consistent-return': 'warn',
    'max-classes-per-file': 'warn',
    'class-methods-use-this': 'warn',
    'guard-for-in': 'warn',
    'default-case': 'warn',

    /**
     * @description rules of eslint-import-resolver-typescript
     */
    'import/no-unresolved': 'error',
    'import/no-extraneous-dependencies': ['error', { devDependencies: ['**/*.spec.ts'] }],

    'import/no-cycle': 'off',
    'import/prefer-default-export': 'off',

    /**
     * @description rules of eslint-plugin-prettier
     */
    // 'prettier/prettier': [
    //   'error',
    //   {
    //     semi: true,
    //     jsxBracketSameLine: true,
    //     trailingComma: 'none',
    //     singleQuote: true,
    //     printWidth: 120,
    //     tabWidth: 2
    //   }
    // ],

    /**
     * @description rules of eslint-plugin-jsx-a11y
     */
    'jsx-a11y/anchor-is-valid': [
      'error',
      {
        components: ['Link'],
        specialLink: ['to', 'hrefLeft', 'hrefRight'],
        aspects: ['noHref', 'invalidHref', 'preferButton']
      }
    ],
    'jsx-a11y/label-has-for': 'off', // deprecated
    'jsx-a11y/label-has-associated-control': 'warn'
  },
  settings: {
    react: {
      version: 'detect' // Tells eslint-plugin-react to automatically detect the version of React to use
      // React version. "detect" automatically picks the version you have installed.
      // You can also use `16.0`, `16.3`, etc, if you want to override the detected value.
      // default to latest and warns if missing
      // It will default to "detect" in the future
    },
    'import/resolver': {
      node: {
        extensions: ['.mjs', '.js', '.json', '.ts', '.tsx']
      }
    }
  }
};
