// Shared DTOs for the Areas feature.
// Keep this file free of Angular/HTTP code so components/services can import types cleanly.

export interface Area {
  id: number;
  name: string;
}

export interface AreaDetail extends Area {
  description?: string;
}
