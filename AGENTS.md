# AGENTS.md

## Avalonia

- In a control's XAML, don't bind to the control itself: no `$parent[Type]`, `RelativeSource AncestorType` or
  `{Binding #Root.X}` on a named root. Name the inner control and bind it in the constructor, after
  `InitializeComponent()`: `Box[!!TextBox.TextProperty] = this[!!TextProperty]` (`!` one way, `!!` two way).
