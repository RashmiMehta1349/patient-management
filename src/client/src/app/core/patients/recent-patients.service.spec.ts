import { TestBed } from '@angular/core/testing';
import { RecentPatientsService } from './recent-patients.service';

describe('RecentPatientsService', () => {
  const STORAGE_KEY = 'pma_recent_patients';

  const makePatient = (id: number, fullName = 'Patient ' + id) => ({
    id,
    fullName,
    countryCode: '+91',
    phoneNumber: '5550000' + id
  });

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
  });

  it('recording a new patient adds it to the front of the list', () => {
    const service = TestBed.inject(RecentPatientsService);
    service.record(makePatient(1));
    service.record(makePatient(2));

    const list = service.list();
    expect(list.length).toBe(2);
    expect(list[0].id).toBe(2);
    expect(list[1].id).toBe(1);
  });

  it('recording a patient already in the list moves it to the front without duplicating', () => {
    const service = TestBed.inject(RecentPatientsService);
    service.record(makePatient(1));
    service.record(makePatient(2));
    service.record(makePatient(3));
    service.record(makePatient(1));

    const list = service.list();
    expect(list.length).toBe(3);
    expect(list[0].id).toBe(1);
    expect(list.map((p) => p.id).sort()).toEqual([1, 2, 3]);
  });

  it('recording beyond the cap (5) evicts the oldest entry', () => {
    const service = TestBed.inject(RecentPatientsService);
    for (let i = 1; i <= 6; i++) {
      service.record(makePatient(i));
    }

    const list = service.list();
    expect(list.length).toBe(5);
    expect(list.map((p) => p.id)).toEqual([6, 5, 4, 3, 2]);
    expect(list.find((p) => p.id === 1)).toBeUndefined();
  });

  it('clear() empties the stored list', () => {
    const service = TestBed.inject(RecentPatientsService);
    service.record(makePatient(1));
    service.clear();

    expect(service.list()).toEqual([]);
  });

  it('reading from a corrupted localStorage value returns an empty list rather than throwing', () => {
    localStorage.setItem(STORAGE_KEY, '{not valid json');
    const service = TestBed.inject(RecentPatientsService);

    expect(() => service.list()).not.toThrow();
    expect(service.list()).toEqual([]);
  });

  it('reading a malformed (non-array) localStorage value returns an empty list', () => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ not: 'an array' }));
    const service = TestBed.inject(RecentPatientsService);

    expect(service.list()).toEqual([]);
  });
});
