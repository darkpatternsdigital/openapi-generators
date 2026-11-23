/** Formats a path, query or form value; `null` (a nullable value that is present) is sent empty. */
export const toParameterString = (value: unknown) =>
	value === null || value === undefined ? '' : String(value);

export const encodeURI = (value: unknown) =>
	encodeURIComponent(toParameterString(value));

export const throwIfNullOrUndefined = (value: unknown, nickname?: string) => {
	if (value == null) {
		throw new Error(
			`Parameter "${value}" was null or undefined when calling "${nickname}".`,
		);
	}
};
