import math

from calculator import calculate_single_weight


def test_rect_plate():
    value = calculate_single_weight("rect_plate", [1000, 1000, 10], 7.85)
    assert math.isclose(value, 78.5, rel_tol=1e-9)


def test_cylinder():
    value = calculate_single_weight("cylinder", [100, 1000], 7.85)
    expected = math.pi * 100**2 * 0.25 * 1000 * 7.85 * 1e-6
    assert math.isclose(value, expected, rel_tol=1e-9)


def test_tube_od_t():
    value = calculate_single_weight("tube_od_t", [100, 10, 1000], 7.85)
    inner = 80
    expected = math.pi * (100**2 - inner**2) * 0.25 * 1000 * 7.85 * 1e-6
    assert math.isclose(value, expected, rel_tol=1e-9)


def test_wire():
    value = calculate_single_weight("wire_by_mpm", [12.3, 100], 7.85)
    assert math.isclose(value, 1230.0, rel_tol=1e-9)
