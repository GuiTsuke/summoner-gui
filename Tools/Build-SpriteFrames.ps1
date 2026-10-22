# Original PNGs are read only. Produces Godot AtlasTexture/SpriteFrames resources.
#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public class SpriteRegion {
 public int Row, Column, X, Y, Width, Height, AnchorX, AnchorY, BoundaryPixels, VerticalBoundaryPixels;
}
public static class SpriteScan {
 public static SpriteRegion[] Read(string path,int columns,int[] rows) {
  using(var bitmap=new Bitmap(path)) {
   int w=bitmap.Width,h=bitmap.Height;
   var data=bitmap.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   var bytes=new byte[data.Stride*h]; Marshal.Copy(data.Scan0,bytes,0,bytes.Length);
   int stride=data.Stride; bitmap.UnlockBits(data);
   Func<int,int,byte> alpha=(x,y)=>bytes[y*stride+x*4+3];
   var result=new List<SpriteRegion>();
   var rowCuts=new int[columns,rows.Length];
   for(int c=0;c<columns;c++) for(int r=0;r<rows.Length;r++) {
    int best=rows[r],score=int.MaxValue;
    if(columns==4 && r>0 && r<rows.Length-1) {
     // Each column can have different feet/particles/crown extents. Search
     // its own transparent valley instead of imposing one horizontal cut.
     for(int y=rows[r]-18;y<=rows[r]+18;y++) { int count=0;
      for(int x=w*c/columns;x<w*(c+1)/columns;x++) if(alpha(x,y)>32) count++;
      int candidate=count*100+Math.Abs(y-rows[r]);
      if(candidate<score) {score=candidate;best=y;}
     }
    }
    rowCuts[c,r]=best;
   }
   for(int row=0;row<rows.Length-1;row++) {
    int top=rows[row],bottom=rows[row+1]; var cuts=new int[columns+1]; cuts[columns]=w;
    for(int c=1;c<columns;c++) {
     int ideal=w*c/columns,best=ideal,score=int.MaxValue;
     for(int x=ideal-18;x<=ideal+18;x++) { int count=0;
      for(int y=top;y<bottom;y++) if(alpha(x,y)>32) count++;
      int candidate=count*100+Math.Abs(x-ideal);
      if(candidate<score) {score=candidate;best=x;}
     } cuts[c]=best;
    }
    for(int c=0;c<columns;c++) {
     top=rowCuts[c,row];bottom=rowCuts[c,row+1];
     int left=cuts[c],right=cuts[c+1],cw=right-left,ch=bottom-top;
     int minX=right,minY=bottom,maxX=left,maxY=top,boundary=0,verticalBoundary=0;
     for(int y=top;y<bottom;y++) for(int x=left;x<right;x++) if(alpha(x,y)>32) {
      minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);
      if(x==left || x==right-1) boundary++;
      if(y==top || y==bottom-1) verticalBoundary++;
     }
     var visited=new bool[cw*ch];var queue=new int[cw*ch];
     int largest=0,ax=(minX+maxX)/2,ay=maxY;
     for(int sy=top;sy<bottom;sy++) for(int sx=left;sx<right;sx++) {
      int start=(sy-top)*cw+sx-left;if(visited[start] || alpha(sx,sy)<128) continue;
      int head=0,tail=1;queue[0]=start;visited[start]=true;int low=sy,high=sy;
      while(head<tail) {int p=queue[head++],px=p%cw,py=p/cw;low=Math.Min(low,py+top);high=Math.Max(high,py+top);
       int[] near={px>0?p-1:-1,px<cw-1?p+1:-1,py>0?p-cw:-1,py<ch-1?p+cw:-1};
       foreach(int n in near) if(n>=0 && !visited[n] && alpha(n%cw+left,n/cw+top)>=128) {visited[n]=true;queue[tail++]=n;}
      }
      if(tail>largest) { largest=tail;ay=high;long sum=0;int count=0;
       int footTop=high-Math.Max(4,(high-low)/6);
       for(int i=0;i<tail;i++) if(queue[i]/cw+top>=footTop) {sum+=queue[i]%cw+left;count++;}
       ax=count>0?(int)(sum/count):(minX+maxX)/2;
      }
     }
     minX=Math.Max(left,minX-1);minY=Math.Max(top,minY-1);maxX=Math.Min(right-1,maxX+1);maxY=Math.Min(bottom-1,maxY+1);
     result.Add(new SpriteRegion{Row=row,Column=c,X=minX,Y=minY,Width=maxX-minX+1,Height=maxY-minY+1,AnchorX=ax,AnchorY=ay,BoundaryPixels=boundary,VerticalBoundaryPixels=verticalBoundary});
    }
   }
   return result.ToArray();
  }
 }
}
'@
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root 'Resources\Animations'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$sheets = @(
 @{Name='bulbasaur-evolucoes';Columns=8;Rows=@(60,330,600,887);Canvas=384},
 @{Name='charmander-evolucoes';Columns=8;Rows=@(20,300,565,887);Canvas=384},
 @{Name='squirtle-evolucoes';Columns=8;Rows=@(60,300,575,887);Canvas=384},
 # V4 sheets have five action rows, but unequal vertical spacing. These
 # boundaries follow the actual transparent gaps rather than height / 5.
 @{Name='slime';Columns=4;Rows=@(0,270,500,705,905,1086);Canvas=512},
 @{Name='goblin';Columns=4;Rows=@(0,295,545,775,1012,1205);Canvas=512},
 @{Name='golem';Columns=4;Rows=@(0,218,426,637,844,1024);Canvas=512},
 @{Name='rei-ogro-boss';Columns=4;Rows=@(0,246,484,701,922,1086);Canvas=512}
)
$inspection = @()
foreach ($sheet in $sheets) {
 $source = Join-Path $root ('Assets\Art\Source\' + $sheet.Name + '.png')
 $regions = [SpriteScan]::Read($source,$sheet.Columns,[int[]]$sheet.Rows)
 $text = [Text.StringBuilder]::new()
 $null = $text.AppendLine('[gd_resource type="SpriteFrames" load_steps=' + ($regions.Count+2) + ' format=3]')
 $null = $text.AppendLine('[ext_resource type="Texture2D" path="res://Assets/Art/Source/' + $sheet.Name + '.png" id="1_sheet"]')
 foreach ($region in $regions) {
  $id = 'r' + $region.Row + 'c' + $region.Column
  $null = $text.AppendLine("`n[sub_resource type=`"AtlasTexture`" id=`"$id`"]")
  $null = $text.AppendLine('atlas = ExtResource("1_sheet")')
  $null = $text.AppendLine("region = Rect2($($region.X), $($region.Y), $($region.Width), $($region.Height))")
  $mx = [int]($sheet.Canvas/2) - ($region.AnchorX-$region.X)
  $my = [int]($sheet.Canvas*0.875) - ($region.AnchorY-$region.Y)
  $null = $text.AppendLine("margin = Rect2($mx, $my, $($sheet.Canvas-$region.Width), $($sheet.Canvas-$region.Height))")
  $null = $text.AppendLine('filter_clip = true')
 }
 $animations = @()
 if ($sheet.Columns -eq 8) {
  for ($r=0;$r -lt 3;$r++) {
   $animations += @{Name="idle_$r";Row=$r;Cols=@(0,1);Loop='true';Fps=2}
   $animations += @{Name="attack_$r";Row=$r;Cols=@(4,5);Loop='false';Fps=8}
   $animations += @{Name="orientation_$r";Row=$r;Cols=@(2,3);Loop='false';Fps=2}
   $animations += @{Name="attack_alt_$r";Row=$r;Cols=@(6,7);Loop='false';Fps=8}
  }
 } else {
  $animations += @{Name='move_right';Row=0;Cols=@(0,1,2,3);Loop='true';Fps=6}
  $animations += @{Name='move_left';Row=1;Cols=@(0,1,2,3);Loop='true';Fps=6}
  $animations += @{Name='move_front';Row=2;Cols=@(0,1,2,3);Loop='true';Fps=6}
  $animations += @{Name='hit';Row=3;Cols=@(0,1,2,3);Loop='false';Fps=10}
  $animations += @{Name='death';Row=4;Cols=@(0,1,2,3);Loop='false';Fps=6}
 }
 $null = $text.AppendLine("`n[resource]`nanimations = [")
 $entries = foreach ($animation in $animations) {
  $frames = foreach ($column in $animation.Cols) { '{"duration": 1.0, "texture": SubResource("r'+$animation.Row+'c'+$column+'")}' }
  '{"frames": [' + ($frames -join ', ') + '], "loop": ' + $animation.Loop + ', "name": &"' + $animation.Name + '", "speed": ' + $animation.Fps + '.0}'
 }
 $null = $text.AppendLine(($entries -join ",`n") + "`n]")
 [IO.File]::WriteAllText((Join-Path $output ($sheet.Name+'.tres')),$text.ToString())
 $bitmap = [Drawing.Bitmap]::new($source)
 $inspection += @{ Name=$sheet.Name; Width=$bitmap.Width; Height=$bitmap.Height; CornerAlpha=$bitmap.GetPixel(0,0).A; Canvas=$sheet.Canvas; Regions=$regions }
 $bitmap.Dispose()
 Write-Output "$($sheet.Name): $($regions.Count) regions, canvas $($sheet.Canvas)."
}
$inspection | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $output 'inspection.json')
