#!/usr/bin/env node

const nodePath = require('path');

const dllPath = nodePath.join(
	__dirname,
	'../dotnet/DarkPatterns.OpenApiCodegen.Client.TypeScript.dll',
);

const [, , ...args] = process.argv;
require('child_process')
	.spawn(`dotnet`, [dllPath, ...args], {
		argv0: 'dotnet',
		stdio: 'inherit',
	})
	.on('close', (code) => {
		process.exit(code);
	});
