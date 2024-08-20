import { fileURLToPath } from 'node:url';
import { includeIgnoreFile } from '@eslint/compat';
import globals from 'globals';
import dpeslint from '@darkpatternsdigital/eslint-config';
import { tseslint } from '@darkpatternsdigital/eslint-config/types';

export default tseslint.config(
	includeIgnoreFile(fileURLToPath(import.meta.resolve('./.gitignore'))),
	...dpeslint,
	// {
	//     languageOptions: {
	//         parserOptions: {
	//             projectService: false,
	//             project: ['**/tsconfig.json', '**/tsconfig.*.json'],
	//         }
	//     }
	// },
	{
		files: ['**/*.{ts,tsx}'],

		rules: {
			'@typescript-eslint/array-type': [
				'error',
				{
					default: 'array-simple',
				},
			],

			'@typescript-eslint/consistent-type-imports': [
				'error',
				{
					disallowTypeAnnotations: false,
				},
			],

			'@typescript-eslint/no-unsafe-assignment': [0],

			'@typescript-eslint/naming-convention': [
				'error',
				{
					selector: 'default',
					format: ['camelCase', 'PascalCase'],
					leadingUnderscore: 'forbid',
				},
				{
					selector: 'class',
					format: ['PascalCase'],
				},
				{
					selector: 'classProperty',
					modifiers: ['private'],
					format: ['camelCase'],
					leadingUnderscore: 'require',
				},
				{
					selector: 'typeParameter',
					format: ['PascalCase'],
					prefix: ['T'],
				},
				{
					selector: 'typeAlias',
					format: ['PascalCase'],
				},
				{
					selector: 'interface',
					format: ['PascalCase'],
				},
				{
					selector: ['objectLiteralProperty', 'import'],
					format: null,
				},
			],

			'import/order': [
				'error',
				{
					pathGroupsExcludedImportTypes: ['react'],
					'newlines-between': 'never',

					alphabetize: {
						order: 'asc',
						caseInsensitive: true,
					},

					groups: [
						'external',
						'builtin',
						'internal',
						'parent',
						'sibling',
						'index',
						'object',
						'unknown',
					],

					pathGroups: [
						{
							pattern: 'react**',
							group: 'external',
							position: 'before',
						},
						{
							pattern: '@!(/)*/**',
							group: 'external',
							position: 'before',
						},
						{
							pattern: '@/**',
							group: 'internal',
							position: 'before',
						},
					],
				},
			],
		},
	},
	{
		files: ['generators/typescript*/**/tests/**/*.{ts,tsx}'],
		rules: {
			'@typescript-eslint/prefer-promise-reject-errors': 'off',
		},
	},
	{
		files: ['generators/typescript*/**/bin/**/*.{js,cjs,mjs}'],
		languageOptions: {
			globals: {
				...globals.node,
			},
		},
		rules: {
			'@typescript-eslint/no-require-imports': 'off',
		},
	},
	{
		files: ['generators/typescript*/**/*.{ts,tsx}'],
		rules: {
			'@typescript-eslint/no-explicit-any': 'off',
			'@typescript-eslint/no-empty-object-type': 'off',
			'@typescript-eslint/no-unused-vars': 'off',
		},
	},
);
