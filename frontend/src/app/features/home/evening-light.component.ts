import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Input,
  NgZone,
  OnDestroy,
  ViewChild,
  inject
} from '@angular/core';

/**
 * Fragment-Shader für Abendlicht, Sonnenstrahlen und Talnebel.
 * Er malt nur Licht auf Schwarz – über das Foto gelegt (mix-blend-mode: screen)
 * bleibt Schwarz unsichtbar und nur das Licht kommt dazu.
 */
const FRAGMENT_SHADER = `#version 300 es
precision highp float;
out vec4 outColor;
uniform float uTime;
uniform vec2 uResolution;
uniform vec2 uPointer;
uniform vec3 uLight;
uniform vec3 uMist;
uniform float uStrength;
uniform float uRays;
uniform float uFog;
float hash(vec2 p) { p = fract(p * vec2(123.34, 456.21)); p += dot(p, p + 45.32); return fract(p.x * p.y); }
float noise(vec2 p) {
  vec2 i = floor(p), f = fract(p), u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
  return mix(mix(hash(i), hash(i + vec2(1, 0)), u.x), mix(hash(i + vec2(0, 1)), hash(i + vec2(1, 1)), u.x), u.y);
}
float fbm(vec2 p) {
  float v = 0.0, a = 0.5; mat2 r = mat2(0.8, 0.6, -0.6, 0.8);
  for (int i = 0; i < 4; i++) { v += a * noise(p); p = r * p * 2.02; a *= 0.5; }
  return v;
}
void main() {
  vec2 uv = gl_FragCoord.xy / uResolution;
  float asp = uResolution.x / uResolution.y;
  vec2 p = (gl_FragCoord.xy - 0.5 * uResolution) / uResolution.y;
  vec2 m = mix(vec2(0.82, 0.95), uPointer, 0.45);
  vec2 sun = (m - 0.5) * vec2(asp, 1.0);
  vec2 d = p - sun;
  float dist = length(d);
  float glow = exp(-dist * 3.0) * 0.45 + exp(-dist * 14.0) * 0.55;
  float ang = atan(d.y, d.x);
  float r1 = noise(vec2(ang * 5.0, uTime * 0.11));
  float r2 = noise(vec2(ang * 11.0 + 4.0, -uTime * 0.07));
  float rays = pow(max(r1 * 0.6 + r2 * 0.4, 0.0), 2.2) * 0.6;
  rays *= exp(-dist * 1.5) * smoothstep(-0.2, 0.7, -d.y / max(dist, 1e-4)) * uRays;
  float m1 = fbm(vec2(p.x * 1.3 + uTime * 0.035, p.y * 3.0 - uTime * 0.012));
  float m2 = fbm(vec2(p.x * 2.7 - uTime * 0.05, p.y * 5.2 + 3.1));
  float mist = smoothstep(0.38, 0.92, m1 * 0.7 + m2 * 0.45) * smoothstep(0.78, 0.0, uv.y) * uFog;
  vec3 col = uLight * (glow + rays * 0.9) + uMist * mist * 0.6;
  col = 1.0 - exp(-col * 1.3);
  col += hash(gl_FragCoord.xy + fract(uTime * 7.0) * 91.0) * 0.028;
  outColor = vec4(col * uStrength, 1.0);
}`;

/** Vertex-Shader: ein einziges Dreieck, das den ganzen Bildschirm abdeckt. */
const VERTEX_SHADER = `#version 300 es
void main() {
  vec2 p = vec2(gl_VertexID << 1 & 2, gl_VertexID & 2);
  gl_Position = vec4(p * 2.0 - 1.0, 0.0, 1.0);
}`;

/**
 * Animiertes Abendlicht über dem Startseiten-Foto (WebGL2).
 *
 * - Läuft außerhalb der Angular-Zone, damit nicht jedes Bild (60x pro Sekunde)
 *   eine Change Detection auslöst.
 * - Pausiert, wenn der Bereich nicht sichtbar ist oder der Tab im Hintergrund liegt.
 * - Bei „weniger Bewegung" (prefers-reduced-motion) wird nur ein ruhiges Standbild gemalt.
 * - Ohne WebGL2 passiert einfach nichts – das Foto bleibt wie es ist.
 */
@Component({
  selector: 'app-evening-light',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<canvas #canvas aria-hidden="true"></canvas>`,
  styles: [`
    :host { display: block; pointer-events: none; }
    canvas { display: block; width: 100%; height: 100%; mix-blend-mode: screen; }
  `]
})
export class EveningLightComponent implements AfterViewInit, OnDestroy {

  /** Farbe des Sonnenlichts (Hex). */
  @Input() lightColor = '#E9A04B';

  /** Farbe des Nebels (Hex). */
  @Input() mistColor = '#D6CFC4';

  /** Gesamthelligkeit des Effekts (0 = aus, 1 = normal). */
  @Input() strength = 0.75;

  /** Stärke der Sonnenstrahlen. */
  @Input() rays = 0.9;

  /** Dichte des Nebels. */
  @Input() fog = 0.8;

  @ViewChild('canvas', { static: true }) private canvasRef!: ElementRef<HTMLCanvasElement>;

  private readonly zone = inject(NgZone);
  private readonly host: ElementRef<HTMLElement> = inject(ElementRef);

  private gl: WebGL2RenderingContext | null = null;
  private program: WebGLProgram | null = null;
  private uniforms: Record<string, WebGLUniformLocation | null> = {};
  private frameId = 0;
  private visible = true;
  private lastFrame = 0;
  private time = 12;
  private pointer = { x: 0.5, y: 0.5 };
  private pointerTarget = { x: 0.5, y: 0.5 };
  private pointerInside = false;
  private observer?: IntersectionObserver;
  private readonly reducedMotion = typeof matchMedia === 'function'
    && matchMedia('(prefers-reduced-motion: reduce)').matches;

  /** Startet WebGL nach dem Rendern der Ansicht. */
  ngAfterViewInit(): void {
    if (!this.setup()) {
      return;
    }

    this.zone.runOutsideAngular(() => {
      // Zeiger über dem ganzen Hero-Bereich verfolgen (der Canvas selbst liegt unter dem Text).
      const area = this.host.nativeElement.parentElement ?? this.host.nativeElement;
      area.addEventListener('pointermove', this.onPointerMove);
      area.addEventListener('pointerleave', this.onPointerLeave);
      document.addEventListener('visibilitychange', this.onVisibilityChange);

      this.observer = new IntersectionObserver(entries => {
        this.visible = entries[entries.length - 1].isIntersecting;
        if (this.visible) {
          this.start();
        } else {
          this.stop();
        }
      });
      this.observer.observe(this.host.nativeElement);

      if (this.reducedMotion) {
        this.draw(performance.now());
      } else {
        this.start();
      }
    });
  }

  /** Räumt Animation, Listener und WebGL-Kontext wieder auf. */
  ngOnDestroy(): void {
    this.stop();
    this.observer?.disconnect();
    const area = this.host.nativeElement.parentElement ?? this.host.nativeElement;
    area.removeEventListener('pointermove', this.onPointerMove);
    area.removeEventListener('pointerleave', this.onPointerLeave);
    document.removeEventListener('visibilitychange', this.onVisibilityChange);
    this.gl?.getExtension('WEBGL_lose_context')?.loseContext();
    this.gl = null;
  }

  /** Erstellt Kontext und Shader-Programm. Gibt false zurück, wenn WebGL2 fehlt oder der Shader nicht kompiliert. */
  private setup(): boolean {
    const gl = this.canvasRef.nativeElement.getContext('webgl2', { alpha: false, antialias: false, premultipliedAlpha: false });
    if (!gl) {
      return false;
    }

    const vertex = this.compile(gl, gl.VERTEX_SHADER, VERTEX_SHADER);
    const fragment = this.compile(gl, gl.FRAGMENT_SHADER, FRAGMENT_SHADER);
    if (!vertex || !fragment) {
      return false;
    }

    const program = gl.createProgram();
    if (!program) {
      return false;
    }
    gl.attachShader(program, vertex);
    gl.attachShader(program, fragment);
    gl.linkProgram(program);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
      console.warn('Abendlicht: Shader konnte nicht verlinkt werden.', gl.getProgramInfoLog(program));
      return false;
    }
    gl.useProgram(program);

    for (const name of ['uTime', 'uResolution', 'uPointer', 'uLight', 'uMist', 'uStrength', 'uRays', 'uFog']) {
      this.uniforms[name] = gl.getUniformLocation(program, name);
    }

    this.gl = gl;
    this.program = program;
    return true;
  }

  /** Kompiliert einen einzelnen Shader und meldet Fehler in der Konsole. */
  private compile(gl: WebGL2RenderingContext, type: number, source: string): WebGLShader | null {
    const shader = gl.createShader(type);
    if (!shader) {
      return null;
    }
    gl.shaderSource(shader, source);
    gl.compileShader(shader);
    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
      console.warn('Abendlicht: Shader-Fehler', gl.getShaderInfoLog(shader));
      return null;
    }
    return shader;
  }

  private start(): void {
    if (this.frameId || !this.gl || this.reducedMotion) {
      return;
    }
    this.lastFrame = performance.now();
    this.frameId = requestAnimationFrame(this.loop);
  }

  private stop(): void {
    cancelAnimationFrame(this.frameId);
    this.frameId = 0;
  }

  /** Animationsschleife: ein Bild malen und das nächste anfordern. */
  private readonly loop = (now: number): void => {
    this.draw(now);
    this.frameId = requestAnimationFrame(this.loop);
  };

  /** Malt ein Bild. Die Auflösung ist halbiert – Licht und Nebel sind weich, das spart viel Rechenleistung. */
  private draw(now: number): void {
    const gl = this.gl;
    const canvas = this.canvasRef.nativeElement;
    if (!gl || !this.program) {
      return;
    }

    const dt = Math.min(0.1, (now - this.lastFrame) / 1000);
    this.lastFrame = now;
    if (!this.reducedMotion) {
      this.time += dt;
    }

    // Ohne Maus wandert die Sonne langsam von selbst.
    if (!this.pointerInside && !this.reducedMotion) {
      this.pointerTarget = {
        x: 0.5 + 0.3 * Math.sin(this.time * 0.37),
        y: 0.5 + 0.24 * Math.sin(this.time * 0.53 + 1.1)
      };
    }
    const ease = 1 - Math.pow(0.9, dt * 60);
    this.pointer.x += (this.pointerTarget.x - this.pointer.x) * ease;
    this.pointer.y += (this.pointerTarget.y - this.pointer.y) * ease;

    const scale = 0.5;
    const width = Math.max(2, Math.round(canvas.clientWidth * scale));
    const height = Math.max(2, Math.round(canvas.clientHeight * scale));
    if (canvas.width !== width || canvas.height !== height) {
      canvas.width = width;
      canvas.height = height;
    }

    gl.viewport(0, 0, width, height);
    gl.uniform1f(this.uniforms['uTime'], this.time);
    gl.uniform2f(this.uniforms['uResolution'], width, height);
    gl.uniform2f(this.uniforms['uPointer'], this.pointer.x, this.pointer.y);
    gl.uniform3fv(this.uniforms['uLight'], this.hexToRgb(this.lightColor));
    gl.uniform3fv(this.uniforms['uMist'], this.hexToRgb(this.mistColor));
    gl.uniform1f(this.uniforms['uStrength'], this.strength);
    gl.uniform1f(this.uniforms['uRays'], this.rays);
    gl.uniform1f(this.uniforms['uFog'], this.fog);
    gl.drawArrays(gl.TRIANGLES, 0, 3);
  }

  private readonly onPointerMove = (event: PointerEvent): void => {
    const rect = this.host.nativeElement.getBoundingClientRect();
    this.pointerInside = true;
    this.pointerTarget = {
      x: (event.clientX - rect.left) / Math.max(rect.width, 1),
      y: 1 - (event.clientY - rect.top) / Math.max(rect.height, 1)
    };
  };

  private readonly onPointerLeave = (): void => {
    this.pointerInside = false;
  };

  private readonly onVisibilityChange = (): void => {
    if (document.hidden) {
      this.stop();
    } else if (this.visible) {
      this.start();
    }
  };

  /** Wandelt "#RRGGBB" in [r, g, b] mit Werten von 0 bis 1 um. */
  private hexToRgb(hex: string): [number, number, number] {
    const value = parseInt(hex.replace('#', ''), 16);
    return [((value >> 16) & 255) / 255, ((value >> 8) & 255) / 255, (value & 255) / 255];
  }
}
