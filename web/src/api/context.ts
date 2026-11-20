import { createContext, useContext } from 'react';
import type { FramelyApi } from './client';

export const ApiContext = createContext<FramelyApi | null>(null);

export function useApi(): FramelyApi {
  const api = useContext(ApiContext);
  if (!api) throw new Error('ApiContext is missing');
  return api;
}
