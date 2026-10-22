#Requires -Version 5.1
# Originals remain read only. Atlas margins preserve one canvas and anchor per animation.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public sealed class ElementRegion {
 public int Row, Column, X, Y, Width, Height, BoundaryPixels;
}
public static class ElementScan {
 public static ElementRegion[] Read(string path) {
  using(var image=new Bitmap(path)) {
   int w=image.Width,h=image.Height;
   if(w!=1536 || h!=1024) throw new Exception("Expected the inspected v5 sheet (1536 x 1024).");
   var bits=image.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   int stride=bits.Stride; var bytes=new byte[stride*h];Marshal.Copy(bits.Scan0,bytes,0,bytes.Length);image.UnlockBits(bits);
   Func<int,int,byte> alpha=(x,y)=>bytes[y*stride+x*4+3];
   var result=new ElementRegion[16];
   for(int row=0;row<4;row++) {
    int top=row*256,bottom=(row+1)*256;
    var cuts=new int[]{0,384,768,1152,1536};
    for(int col=1;col<4;col++) {
     int ideal=384*col,best=ideal,score=int.MaxValue;
     for(int x=ideal-64;x<=ideal+64;x++) {
      int count=0;for(int y=top;y<bottom;y++) if(alpha(x,y)>32) count++;
      int candidate=count*1000+Math.Abs(x-ideal);
      if(candidate<score) {score=candidate;best=x;}
     } cuts[col]=best;
    }
    for(int col=0;col<4;col++) {
     int left=cuts[col],right=cuts[col+1],minX=right,minY=bottom,maxX=left,maxY=top,boundary=0;
     for(int y=top;y<bottom;y++) for(int x=left;x<right;x++) if(alpha(x,y)>32) {
      minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);
      if((x==left && left>0 && alpha(x-1,y)>32) || (x==right-1 && right<w && alpha(x+1,y)>32) || (y==top && top>0 && alpha(x,y-1)>32) || (y==bottom-1 && bottom<h && alpha(x,y+1)>32)) boundary++;
     }
     if(minX>maxX || minY>maxY) throw new Exception("Empty effect frame.");
     minX=Math.Max(left,minX-1);minY=Math.Max(top,minY-1);maxX=Math.Min(right-1,maxX+1);maxY=Math.Min(bottom-1,maxY+1);
     result[row*4+col]=new ElementRegion{Row=row,Column=col,X=minX,Y=minY,Width=maxX-minX+1,Height=maxY-minY+1,BoundaryPixels=boundary};
    }
   } return result;
  }
 }
}
'@
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'Assets\Art\Effects\efeitos-elementais.png'
if (-not (Test-Path -LiteralPath $source)) { throw 'Copy the v5 effect sheet to Assets/Art/Effects first.' }
$regions = [ElementScan]::Read($source)
# Sheet-space anchors were inspected against the opaque artwork. X/Y remain
# independent of transparent fringe bounds. Water's impact is on the left.
$rows = @(
 @{Name='fireball';CanvasAnchor=@(448,128);AnchorX=@(241,659,1081,1439);AnchorY=@(134,134,134,134);Direction='right';FlipH=$false;Scale=0.14;Role='projectile_head';Loop='true';Fps=12},
 @{Name='vine_whips';CanvasAnchor=@(64,144);AnchorX=@(51,402,800,1247);AnchorY=@(400,411,411,406);Direction='right';FlipH=$false;Scale=0.20;Role='paired_vine_roots';Loop='false';Fps=12},
 @{Name='water_jet';CanvasAnchor=@(64,128);AnchorX=@(66,391,805,1259);AnchorY=@(648,648,648,648);Direction='left';FlipH=$true;Scale=0.20;Role='jet_impact';Loop='true';Fps=12},
 @{Name='burn';CanvasAnchor=@(256,224);AnchorX=@(168,555,958,1360);AnchorY=@(987,987,987,987);Direction='up';FlipH=$false;Scale=0.12;Role='flame_base';Loop='true';Fps=8}
)
$text = [Text.StringBuilder]::new()
$null = $text.AppendLine('[gd_resource type="SpriteFrames" load_steps=18 format=3]')
$null = $text.AppendLine('[ext_resource type="Texture2D" path="res://Assets/Art/Effects/efeitos-elementais.png" id="1_sheet"]')
$metadata = @()
foreach ($region in $regions) {
 $row = $rows[$region.Row]
 $anchorX = $row.AnchorX[$region.Column]
 $anchorY = $row.AnchorY[$region.Column]
 $mx = $row.CanvasAnchor[0] - ($anchorX - $region.X)
 $my = $row.CanvasAnchor[1] - ($anchorY - $region.Y)
 if ($mx -lt 0 -or $my -lt 0 -or ($mx+$region.Width) -gt 512 -or ($my+$region.Height) -gt 256) { throw "Inspected anchor no longer fits canvas: $($row.Name) $($region.Column) x=$mx y=$my size=$($region.Width),$($region.Height)" }
 $null = $text.AppendLine("`n[sub_resource type=`"AtlasTexture`" id=`"r$($region.Row)c$($region.Column)`"]")
 $null = $text.AppendLine('atlas = ExtResource("1_sheet")')
 $null = $text.AppendLine("region = Rect2($($region.X), $($region.Y), $($region.Width), $($region.Height))")
 $null = $text.AppendLine("margin = Rect2($mx, $my, $(512-$region.Width), $(256-$region.Height))")
 $null = $text.AppendLine('filter_clip = true')
 $metadata += [ordered]@{Animation=$row.Name;Frame=$region.Column;Region=@($region.X,$region.Y,$region.Width,$region.Height);SheetAnchor=@($anchorX,$anchorY);BoundaryPixels=$region.BoundaryPixels}
}
$null = $text.AppendLine("`n[resource]`nanimations = [")
$entries = for ($r=0;$r -lt 4;$r++) {
 $row=$rows[$r]
 $frames=0..3 | ForEach-Object { '{"duration": 1.0, "texture": SubResource("r'+$r+'c'+$_+'")}' }
 '{"frames": ['+($frames -join ', ')+'], "loop": '+$row.Loop+', "name": &"'+$row.Name+'", "speed": '+$row.Fps+'.0}'
}
$null = $text.AppendLine(($entries -join ",`n") + "`n]")
$output=Join-Path $root 'Resources\Animations'
[IO.File]::WriteAllText((Join-Path $output 'elemental-effects.tres'),$text.ToString())
$inspection=[ordered]@{
 Source='Assets/Art/Effects/efeitos-elementais.png';SHA256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash;Width=1536;Height=1024;CornerAlpha=0;Canvas=@(512,256)
 AlphaThreshold=32;BoundarySearchPixels=64
 SpriteOffsetFormula='Canvas / 2 - CanvasAnchor. Apply before scale; negate X for FlipH.'
 Notes=@('Regions bound alpha above 32; faint alpha fringes may extend beyond these bounds. RGB in fully transparent pixels is ignored.','Water has its wide impact on the LEFT; flip horizontally to aim right.','Vine sprites already contain a pair of branches, not a single straight beam.','Four authored phases per row only; no inferred frames.','Atlas does not provide collision or damage rules.')
 Animations=@($rows | ForEach-Object {[ordered]@{Name=$_.Name;CanvasAnchor=$_.CanvasAnchor;NativeDirection=$_.Direction;FlipHToAimRight=$_.FlipH;SuggestedUniformScale=$_.Scale;AnchorRole=$_.Role}})
 Frames=$metadata
}
$inspection | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $output 'effects-inspection.json')
Write-Output 'Generated 16 individually inspected effect frames; originals unchanged.'
