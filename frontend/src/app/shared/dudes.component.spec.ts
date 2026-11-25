import { dudesLabel, isFullyBooked, needsDudes } from './dudes.component';

describe('dudesLabel', () => {
  it('zeigt angemeldet / Maximum', () => {
    expect(dudesLabel(2, 4)).toBe('2/4');
  });

  it('zeigt ∞ ohne Maximum (Open End)', () => {
    expect(dudesLabel(2, null)).toBe('2/∞');
    expect(dudesLabel(3, undefined)).toBe('3/∞');
  });

  it('zählt fehlende Teilnehmerzahl als 0', () => {
    expect(dudesLabel(undefined, 4)).toBe('0/4');
  });
});

describe('isFullyBooked', () => {
  it('ist ausgebucht, wenn das Maximum erreicht ist', () => {
    expect(isFullyBooked(4, 4)).toBeTrue();
  });

  it('ist nicht ausgebucht darunter oder ohne Maximum', () => {
    expect(isFullyBooked(3, 4)).toBeFalse();
    expect(isFullyBooked(10, null)).toBeFalse();
  });
});

describe('needsDudes', () => {
  it('braucht Leute, solange das Minimum nicht erreicht ist', () => {
    expect(needsDudes(1, 2)).toBeTrue();
    expect(needsDudes(2, 3)).toBeTrue();
  });

  it('braucht keine Leute mehr ab dem Minimum', () => {
    expect(needsDudes(2, 2)).toBeFalse();
    expect(needsDudes(5, 3)).toBeFalse();
  });

  it('nimmt ohne Minimum den Standard 2', () => {
    expect(needsDudes(1, null)).toBeTrue();
    expect(needsDudes(2, undefined)).toBeFalse();
  });
});
