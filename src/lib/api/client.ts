import type { ApiError } from '@/types';

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';

export class ApiRequestError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ApiError,
  ) {
    super(problem.detail ?? problem.title);
    this.name = 'ApiRequestError';
  }
}

type RequestOptions = Omit<RequestInit, 'body'> & {
  params?: Record<string, string | number | boolean | undefined | null>;
  body?: unknown;
};

function buildUrl(path: string, params?: RequestOptions['params']): string {
  const url = new URL(path, BASE_URL);
  if (params) {
    for (const [key, value] of Object.entries(params)) {
      if (value !== undefined && value !== null) {
        url.searchParams.set(key, String(value));
      }
    }
  }
  return url.toString();
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { params, body, headers, ...rest } = options;

  const init: RequestInit = {
    ...rest,
    credentials: 'include', // send session cookie on every request
    headers: {
      'Content-Type': 'application/json',
      ...headers,
    },
  };

  if (body !== undefined) {
    init.body = JSON.stringify(body);
  }

  const url = buildUrl(path, params);
  const res = await fetch(url, init);

  if (!res.ok) {
    let problem: ApiError;
    try {
      problem = await res.json();
    } catch {
      problem = { title: res.statusText, status: res.status };
    }
    throw new ApiRequestError(res.status, problem);
  }

  // 204 No Content
  if (res.status === 204) {
    return undefined as T;
  }

  return res.json() as Promise<T>;
}

export const apiClient = {
  get: <T>(path: string, params?: RequestOptions['params']) =>
    request<T>(path, { method: 'GET', params }),

  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body }),

  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'PUT', body }),

  delete: <T>(path: string) =>
    request<T>(path, { method: 'DELETE' }),

  postForm: <T>(path: string, formData: FormData) =>
    request<T>(path, {
      method: 'POST',
      // Don't set Content-Type — let browser set multipart boundary
      headers: {},
      body: formData as unknown,
    }),
};
