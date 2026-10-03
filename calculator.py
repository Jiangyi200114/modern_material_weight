from __future__ import annotations

from dataclasses import dataclass
from math import pi
from typing import Dict, List


MATERIAL_DENSITY: Dict[str, float] = {
    "碳钢": 7.85,
    "铜": 8.9,
    "灰铸铁": 7.2,
    "不锈钢": 7.9,
    "铝": 2.7,
    "铸钢": 7.8,
    "其它": 7.8,
}


@dataclass(frozen=True)
class ShapeDefinition:
    key: str
    label: str
    fields: List[str]


SHAPES: List[ShapeDefinition] = [
    ShapeDefinition("rect_plate", "矩形板材", ["长度(mm)", "宽度(mm)", "厚度(mm)"]),
    ShapeDefinition("cylinder", "圆柱体", ["直径(mm)", "长度(mm)"]),
    ShapeDefinition("tube_od_id", "外/内径管材", ["外径(mm)", "内径(mm)", "长度(mm)"]),
    ShapeDefinition("tube_od_t", "外径×壁厚管材", ["外径(mm)", "壁厚(mm)", "长度(mm)"]),
    ShapeDefinition("square_bar", "正方形截面形材", ["边长(mm)", "长度(mm)"]),
    ShapeDefinition("cone", "圆锥体", ["底半径(mm)", "高度(mm)"]),
    ShapeDefinition("frustum", "圆台", ["大半径(mm)", "小半径(mm)", "高度(mm)"]),
    ShapeDefinition("wire_by_mpm", "每米重量线材", ["每米重(kg/m)", "长度(m)"]),
]


def get_shape_by_label(label: str) -> ShapeDefinition:
    for shape in SHAPES:
        if shape.label == label:
            return shape
    raise ValueError("未找到对应形状")


def _positive(value: float, field: str) -> None:
    if value <= 0:
        raise ValueError(f"{field} 必须大于 0")


def calculate_single_weight(shape_key: str, params: List[float], density: float) -> float:
    _positive(density, "密度")
    if shape_key == "rect_plate":
        if len(params) != 3:
            raise ValueError("矩形板材需要 3 个参数")
        length, width, thickness = params
        _positive(length, "长度")
        _positive(width, "宽度")
        _positive(thickness, "厚度")
        return length * width * thickness * density * 1e-6
    if shape_key == "cylinder":
        if len(params) != 2:
            raise ValueError("圆柱体需要 2 个参数")
        diameter, length = params
        _positive(diameter, "直径")
        _positive(length, "长度")
        return pi * (diameter**2) * 0.25 * length * density * 1e-6
    if shape_key == "tube_od_id":
        if len(params) != 3:
            raise ValueError("外/内径管材需要 3 个参数")
        od, inner_d, length = params
        _positive(od, "外径")
        _positive(inner_d, "内径")
        _positive(length, "长度")
        if inner_d >= od:
            raise ValueError("内径必须小于外径")
        return pi * (od**2 - inner_d**2) * 0.25 * length * density * 1e-6
    if shape_key == "tube_od_t":
        if len(params) != 3:
            raise ValueError("外径×壁厚管材需要 3 个参数")
        od, thickness, length = params
        _positive(od, "外径")
        _positive(thickness, "壁厚")
        _positive(length, "长度")
        inner_d = od - 2 * thickness
        if inner_d <= 0:
            raise ValueError("壁厚过大，导致内径小于等于 0")
        return pi * (od**2 - inner_d**2) * 0.25 * length * density * 1e-6
    if shape_key == "square_bar":
        if len(params) != 2:
            raise ValueError("正方形截面形材需要 2 个参数")
        side, length = params
        _positive(side, "边长")
        _positive(length, "长度")
        return side * side * length * density * 1e-6
    if shape_key == "cone":
        if len(params) != 2:
            raise ValueError("圆锥体需要 2 个参数")
        radius, height = params
        _positive(radius, "底半径")
        _positive(height, "高度")
        return pi * radius * radius * height * density * 1e-6 / 3
    if shape_key == "frustum":
        if len(params) != 3:
            raise ValueError("圆台需要 3 个参数")
        big_r, small_r, height = params
        _positive(big_r, "大半径")
        _positive(small_r, "小半径")
        _positive(height, "高度")
        if small_r >= big_r:
            raise ValueError("小半径必须小于大半径")
        return pi * height * (big_r**2 + big_r * small_r + small_r**2) * density * 1e-6 / 3
    if shape_key == "wire_by_mpm":
        if len(params) != 2:
            raise ValueError("每米重量线材需要 2 个参数")
        weight_per_meter, length_m = params
        _positive(weight_per_meter, "每米重")
        _positive(length_m, "长度")
        return weight_per_meter * length_m
    raise ValueError("不支持的形状")
