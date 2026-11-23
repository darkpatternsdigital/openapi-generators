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

/** The media type of a Content-Type header, without parameters and in lowercase; '' when absent. */
export const toMediaType = (contentType: string | null | undefined) =>
	(contentType ?? '').split(';')[0].trim().toLowerCase();

/** `application/json` or a `+json` media type: read as JSON. */
export const isJsonMediaType = (mediaType: string) =>
	mediaType === 'application/json' || mediaType.endsWith('+json');

/** `text/*`: read as a string. Other media types are read as a Blob. */
export const isTextMediaType = (mediaType: string) =>
	mediaType.startsWith('text/');
