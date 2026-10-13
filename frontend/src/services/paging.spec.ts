import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { fetchAllPages } from './paging';

describe('fetchAllPages', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lädt alle Seiten nach und hängt die Einträge aneinander', () => {
    let result: number[] = [];
    fetchAllPages<number>(http, '/api/routes?scale=french').subscribe(items => result = items);

    httpMock.expectOne('/api/routes?scale=french&page=1&pageSize=100')
      .flush({ total: 150, page: 1, pageSize: 100, items: Array.from({ length: 100 }, (_, i) => i) });
    httpMock.expectOne('/api/routes?scale=french&page=2&pageSize=100')
      .flush({ total: 150, page: 2, pageSize: 100, items: Array.from({ length: 50 }, (_, i) => 100 + i) });

    expect(result.length).toBe(150);
    expect(result[149]).toBe(149);
  });

  it('fragt bei nur einer Seite kein zweites Mal an', () => {
    let result: string[] = [];
    fetchAllPages<string>(http, '/api/areas').subscribe(items => result = items);

    httpMock.expectOne('/api/areas?page=1&pageSize=100')
      .flush({ total: 2, page: 1, pageSize: 100, items: ['a', 'b'] });

    expect(result).toEqual(['a', 'b']);
  });
});
