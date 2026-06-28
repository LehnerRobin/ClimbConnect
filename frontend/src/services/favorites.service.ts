import { Injectable } from '@angular/core';

const STORAGE_KEY = 'cc_favorite_areas';

@Injectable({ providedIn: 'root' })
export class FavoritesService {

  private ids: Set<number>;

  constructor() {
    const raw = localStorage.getItem(STORAGE_KEY);
    this.ids = raw ? new Set(JSON.parse(raw)) : new Set();
  }

  isFavorite(areaId: number): boolean {
    return this.ids.has(areaId);
  }

  toggle(areaId: number): void {
    if (this.ids.has(areaId)) {
      this.ids.delete(areaId);
    } else {
      this.ids.add(areaId);
    }
    localStorage.setItem(STORAGE_KEY, JSON.stringify([...this.ids]));
  }

  get favoriteIds(): number[] {
    return [...this.ids];
  }
}
