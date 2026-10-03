from __future__ import annotations

import tkinter as tk
from tkinter import messagebox, ttk

from calculator import MATERIAL_DENSITY, SHAPES, calculate_single_weight, get_shape_by_label


class ModernMaterialWeightApp:
    def __init__(self, root: tk.Tk) -> None:
        self.root = root
        self.root.title("规则材料重量计算器 - 现代版")
        self.root.geometry("980x640")
        self.root.minsize(900, 560)
        self.root.configure(bg="#f3f6fb")
        self.style = ttk.Style()
        self.style.theme_use("clam")
        self.style.configure("Card.TFrame", background="#ffffff")
        self.style.configure("Title.TLabel", background="#f3f6fb", font=("Microsoft YaHei UI", 18, "bold"), foreground="#1f2a44")
        self.style.configure("Sub.TLabel", background="#f3f6fb", font=("Microsoft YaHei UI", 10), foreground="#5b6780")
        self.style.configure("Field.TLabel", background="#ffffff", font=("Microsoft YaHei UI", 10), foreground="#2b3550")
        self.style.configure("ResultTitle.TLabel", background="#ffffff", font=("Microsoft YaHei UI", 11), foreground="#5b6780")
        self.style.configure("ResultValue.TLabel", background="#ffffff", font=("Microsoft YaHei UI", 19, "bold"), foreground="#0e8f5b")
        self.style.configure("TButton", font=("Microsoft YaHei UI", 10))
        self.style.map("Accent.TButton", background=[("active", "#1f7af8"), ("!active", "#2d8cff")], foreground=[("!disabled", "#ffffff")])
        self.material_var = tk.StringVar(value="碳钢")
        self.shape_var = tk.StringVar(value=SHAPES[0].label)
        self.density_var = tk.StringVar(value=str(MATERIAL_DENSITY["碳钢"]))
        self.quantity_var = tk.StringVar(value="1")
        self.param_vars = [tk.StringVar(value="") for _ in range(3)]
        self.single_var = tk.StringVar(value="0.000")
        self.total_var = tk.StringVar(value="0.000")
        self.param_labels: list[ttk.Label] = []
        self.param_entries: list[ttk.Entry] = []
        self._build_ui()
        self._on_material_changed()
        self._on_shape_changed()

    def _build_ui(self) -> None:
        shell = ttk.Frame(self.root, style="Card.TFrame")
        shell.pack(fill="both", expand=True, padx=20, pady=20)
        title_wrap = ttk.Frame(shell)
        title_wrap.pack(fill="x", padx=24, pady=(18, 8))
        ttk.Label(title_wrap, text="规则材料重量计算器（现代版）", style="Title.TLabel").pack(anchor="w")
        ttk.Label(
            title_wrap,
            text="支持 8 类规则材料，自动切换参数，快速计算单件重量与总重量",
            style="Sub.TLabel",
        ).pack(anchor="w", pady=(4, 0))
        body = ttk.Frame(shell, style="Card.TFrame")
        body.pack(fill="both", expand=True, padx=24, pady=(6, 20))
        body.columnconfigure(0, weight=3)
        body.columnconfigure(1, weight=2)
        left = ttk.Frame(body, style="Card.TFrame")
        left.grid(row=0, column=0, sticky="nsew", padx=(0, 14))
        right = ttk.Frame(body, style="Card.TFrame")
        right.grid(row=0, column=1, sticky="nsew")
        self._build_input_panel(left)
        self._build_result_panel(right)

    def _build_input_panel(self, parent: ttk.Frame) -> None:
        row = 0
        ttk.Label(parent, text="材料类型", style="Field.TLabel").grid(row=row, column=0, sticky="w", pady=(0, 8))
        material_combo = ttk.Combobox(parent, textvariable=self.material_var, state="readonly", values=list(MATERIAL_DENSITY.keys()), width=26)
        material_combo.grid(row=row, column=1, sticky="ew", pady=(0, 8))
        material_combo.bind("<<ComboboxSelected>>", lambda _e: self._on_material_changed())
        row += 1
        ttk.Label(parent, text="形状类型", style="Field.TLabel").grid(row=row, column=0, sticky="w", pady=8)
        shape_combo = ttk.Combobox(parent, textvariable=self.shape_var, state="readonly", values=[x.label for x in SHAPES], width=26)
        shape_combo.grid(row=row, column=1, sticky="ew", pady=8)
        shape_combo.bind("<<ComboboxSelected>>", lambda _e: self._on_shape_changed())
        row += 1
        ttk.Label(parent, text="密度(g/cm³)", style="Field.TLabel").grid(row=row, column=0, sticky="w", pady=8)
        self.density_entry = ttk.Entry(parent, textvariable=self.density_var, width=28)
        self.density_entry.grid(row=row, column=1, sticky="ew", pady=8)
        row += 1
        ttk.Label(parent, text="数量(件)", style="Field.TLabel").grid(row=row, column=0, sticky="w", pady=8)
        ttk.Entry(parent, textvariable=self.quantity_var, width=28).grid(row=row, column=1, sticky="ew", pady=8)
        row += 1
        self.param_box = ttk.LabelFrame(parent, text="几何参数")
        self.param_box.grid(row=row, column=0, columnspan=2, sticky="ew", pady=(14, 14))
        self.param_box.columnconfigure(1, weight=1)
        for idx in range(3):
            lbl = ttk.Label(self.param_box, text=f"参数{idx + 1}", style="Field.TLabel")
            lbl.grid(row=idx, column=0, sticky="w", padx=10, pady=8)
            ent = ttk.Entry(self.param_box, textvariable=self.param_vars[idx], width=24)
            ent.grid(row=idx, column=1, sticky="ew", padx=10, pady=8)
            self.param_labels.append(lbl)
            self.param_entries.append(ent)
        row += 1
        btn_row = ttk.Frame(parent, style="Card.TFrame")
        btn_row.grid(row=row, column=0, columnspan=2, sticky="ew", pady=(6, 0))
        ttk.Button(btn_row, text="开始计算", style="Accent.TButton", command=self.calculate).pack(side="left")
        ttk.Button(btn_row, text="清空输入", command=self.clear_inputs).pack(side="left", padx=10)
        ttk.Button(btn_row, text="退出", command=self.root.destroy).pack(side="right")
        parent.columnconfigure(1, weight=1)

    def _build_result_panel(self, parent: ttk.Frame) -> None:
        result_card = ttk.LabelFrame(parent, text="计算结果")
        result_card.pack(fill="x", pady=(0, 12))
        ttk.Label(result_card, text="单件重量 (kg)", style="ResultTitle.TLabel").pack(anchor="w", padx=14, pady=(12, 2))
        ttk.Label(result_card, textvariable=self.single_var, style="ResultValue.TLabel").pack(anchor="w", padx=14, pady=(0, 8))
        ttk.Separator(result_card, orient="horizontal").pack(fill="x", padx=14, pady=8)
        ttk.Label(result_card, text="总重量 (kg)", style="ResultTitle.TLabel").pack(anchor="w", padx=14, pady=(4, 2))
        ttk.Label(result_card, textvariable=self.total_var, style="ResultValue.TLabel").pack(anchor="w", padx=14, pady=(0, 14))
        hint_card = ttk.LabelFrame(parent, text="使用说明")
        hint_card.pack(fill="both", expand=True)
        hints = [
            "1) 单位按输入框标注填写",
            "2) 数量默认 1，可输入小数",
            "3) 选择“其它”材料时可手工输入密度",
            "4) 管材和圆台会进行几何关系校验",
        ]
        for line in hints:
            ttk.Label(hint_card, text=line, style="Field.TLabel").pack(anchor="w", padx=12, pady=6)

    def _on_material_changed(self) -> None:
        material = self.material_var.get()
        preset = MATERIAL_DENSITY.get(material, 7.8)
        if material != "其它":
            self.density_var.set(str(preset))
            self.density_entry.configure(state="readonly")
        else:
            if not self.density_var.get().strip():
                self.density_var.set(str(preset))
            self.density_entry.configure(state="normal")

    def _on_shape_changed(self) -> None:
        shape = get_shape_by_label(self.shape_var.get())
        for idx in range(3):
            if idx < len(shape.fields):
                self.param_labels[idx].configure(text=shape.fields[idx])
                self.param_labels[idx].grid()
                self.param_entries[idx].grid()
            else:
                self.param_labels[idx].grid_remove()
                self.param_entries[idx].grid_remove()
                self.param_vars[idx].set("")

    def clear_inputs(self) -> None:
        for value in self.param_vars:
            value.set("")
        self.quantity_var.set("1")
        self.single_var.set("0.000")
        self.total_var.set("0.000")

    def _to_float(self, text: str, field: str) -> float:
        try:
            value = float(text.strip())
        except Exception as exc:
            raise ValueError(f"{field} 不是有效数字") from exc
        return value

    def calculate(self) -> None:
        try:
            shape = get_shape_by_label(self.shape_var.get())
            density = self._to_float(self.density_var.get(), "密度")
            quantity = self._to_float(self.quantity_var.get(), "数量")
            if quantity <= 0:
                raise ValueError("数量必须大于 0")
            params: list[float] = []
            for idx, name in enumerate(shape.fields):
                params.append(self._to_float(self.param_vars[idx].get(), name))
            single = calculate_single_weight(shape.key, params, density)
            total = single * quantity
            self.single_var.set(f"{single:.6f}".rstrip("0").rstrip("."))
            self.total_var.set(f"{total:.6f}".rstrip("0").rstrip("."))
        except ValueError as exc:
            messagebox.showerror("输入有误", str(exc))


def main() -> None:
    root = tk.Tk()
    ModernMaterialWeightApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
