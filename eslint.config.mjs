import { fileURLToPath } from 'node:url';
import { includeIgnoreFile } from '@eslint/compat';
import globals from 'globals';
import dpeslint from '@darkpatternsdigital/eslint-config';
import { tseslint } from '@darkpatternsdigital/eslint-config/types';

export default tseslint.config(
	includeIgnoreFile(fileURLToPath(import.meta.resolve('./.gitignore'))),
	...dpeslint,
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
			'@typescript-eslint/no-unsafe-assignment': [0],
			'@typescript-eslint/no-unused-vars': 'off',
		},
	},
);
