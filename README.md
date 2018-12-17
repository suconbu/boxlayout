# 属性

属性名  | 説明  | 指定
-------|-------|-------
name   | 名前(重複不可)(必須) | string 
position | 上下左右位置指定 | {top} {right} {bottom} {left}
size   | 幅高さ指定   | {width} {height}
center | 中心位置指定(position指定は無視) | {horizontal} {vertical}
data   | 任意の文字列 | string

# 数値単位

単位 | 説明
-----|--
px   | ピクセル
%    | 親要素の幅/高さに対する割合(百分率)
vw   | コンテナの幅に対する割合(百分率)
vh   | コンテナの高さに対する割合(百分率)
vmax | コンテナの長辺に対する割合(百分率)
vmin | コンテナの短辺に対する割合(百分率)

※ 数値が 0 の場合のみ単位指定を省略可能

# ファイル記述例

## XML

```
<?xml version="1.0" encoding="utf-8"?>
<root>
  <box name="a" data="Red" position="50px">
    <box name="a.a" data="Orange" size-width="50px" size-height="50px"></box>
    <box name="a.b" data="Orange" size="50px 50px" position-top="50px" position-right="50px"/>
    <box name="a.c" data="Orange" size="50px 50px" position-bottom="50px" position-left="50px"/>
    <box name="a.d" data="Orange" size="50px 50px" position="100px 100px 100px 100px"/>
    <box name="a.e" data="Orange" size="50px 50px" center="50%" position-top="100px" position-right="100px" position-bottom="100px" position-left="100px"/>
  </box>
  <box name="b" data="Blue" position="20vh 20vw">
    <box name="b.a" data="RoyalBlue" size="100px" position-top="-50px" position-left="+50px"/>
  </box>
</root>
```

## JSON

```
[
  {
    "name": "a",
    "data": "Red",
    "position": "50px",
    "children": [
      {
        "name": "a.a",
        "data": "Orange",
        "size-width": "50px",
        "size-height": "50px"
      },
      {
        "name": "a.b",
        "data": "Orange",
        "size": "50px 50px",
        "position-top": "50px",
        "position-right": "50px"
      },
      {
        "name": "a.c",
        "data": "Orange",
        "size": "50px 50px",
        "position-bottom": "50px",
        "position-left": "50px"
      },
      {
        "name": "a.d",
        "data": "Orange",
        "size": "50px 50px",
        "position": "100px 100px 100px 100px"
      },
      {
        "name": "a.e",
        "data": "Orange",
        "size": "50px 50px",
        "center": "50%",
        "position-top": "100px",
        "position-right": "100px",
        "position-bottom": "100px",
        "position-left": "100px"
      }
    ]
  },
  {
    "name": "b",
    "data": "Blue",
    "position": "20vh 20vw",
    "children": [
      {
        "name": "b.a",
        "data": "RoyalBlue",
        "size": "100px",
        "position-top": "-50px",
        "position-left": "+50px"
      }
    ]
  }
]
```
