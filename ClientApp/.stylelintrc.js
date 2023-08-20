module.exports = 
{
	// "extends": ["stylelint-prettier/recommended"] does three things:
	// 1. Enables the stylelint-plugin-prettier plugin.
	// 2. Enables the prettier/prettier rule.
	// 3. Extends the stylelint-config-prettier configuration.
	"extends": ["stylelint-prettier/recommended"],

	// disabling the prettier rules as they don't work with the new vscode prettier plugin
	// "rules": {
  //   "prettier/prettier": [true, {"singleQuote": true, "tabWidth": 4}]
  // }
}
