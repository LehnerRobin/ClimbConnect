import { Component, Input, OnChanges, OnDestroy, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { Router } from '@angular/router';
import * as L from 'leaflet';
import { Area } from '../../../services/areas.service';

// Leaflet sucht die Marker-Icons standardmäßig über einen relativen Pfad,
// der beim Angular-Build nicht mitkopiert wird. Deshalb zeigen wir explizit
// auf die Bilder, die wir über angular.json nach /leaflet kopieren lassen.
//
// Neues Design: eigener Pin als SVG (divIcon) statt des blauen Leaflet-Markers.
// Das HTML ist fest im Code und enthält keine Nutzerdaten.
const pinIcon = L.divIcon({
  className: 'area-pin',
  html: '<svg width="26" height="34" viewBox="0 0 26 34" aria-hidden="true">'
    + '<path d="M13 33s11-11 11-20A11 11 0 0 0 2 13c0 9 11 20 11 20Z" fill="#1C1A16"/>'
    + '<circle cx="13" cy="13" r="4.5" fill="#E9A04B"/></svg>',
  iconSize: [26, 34],
  iconAnchor: [13, 34],
  tooltipAnchor: [0, -36]
});

/** Zeigt alle Gebiete mit Koordinaten auf einer Leaflet-Karte an. Klick auf einen Marker öffnet die Gebiets-Detailseite. */
@Component({
  selector: 'app-area-map',
  standalone: true,
  templateUrl: './area-map.component.html',
  styleUrl: './area-map.component.css'
})
export class AreaMapComponent implements AfterViewInit, OnChanges, OnDestroy {

  @Input() areas: Area[] = [];

  @ViewChild('mapContainer', { static: true }) mapContainer!: ElementRef<HTMLDivElement>;

  private map?: L.Map;
  private markers: L.Marker[] = [];
  private viewReady = false;

  constructor(private router: Router) {}

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.initMap();
    this.renderMarkers();
  }

  ngOnChanges(): void {
    if (this.viewReady) {
      this.renderMarkers();
    }
  }

  ngOnDestroy(): void {
    this.map?.remove();
  }

  private initMap(): void {
    // Mittelpunkt Oberösterreich als Standardansicht, bevor Marker das Zoomen übernehmen
    this.map = L.map(this.mapContainer.nativeElement).setView([48.1, 14.2], 8);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
      maxZoom: 18
    }).addTo(this.map);
  }

  private renderMarkers(): void {
    if (!this.map) return;

    this.markers.forEach(marker => marker.remove());
    this.markers = [];

    const areasWithCoords = this.areas.filter(a => a.latitude != null && a.longitude != null);
    if (areasWithCoords.length === 0) return;

    for (const area of areasWithCoords) {
      // Namen dauerhaft als Etikett über dem Pin anzeigen (neues Design)
      const marker = L.marker([area.latitude!, area.longitude!], { icon: pinIcon })
        .addTo(this.map)
        .bindTooltip(this.createTooltipContent(area.name), {
          direction: 'top',
          permanent: true,
          className: 'area-tooltip'
        });

      marker.on('click', () => this.router.navigate(['/areas', area.id]));
      this.markers.push(marker);
    }

    const bounds = L.latLngBounds(areasWithCoords.map(a => [a.latitude!, a.longitude!] as [number, number]));
    this.map.fitBounds(bounds, { padding: [40, 40], maxZoom: 12 });
  }

  /** Baut den Tooltip-Inhalt als reines Text-Element, damit der Gebietsname nie als HTML ausgeführt wird. */
  private createTooltipContent(name: string): HTMLElement {
    const label = document.createElement('span');
    label.textContent = name;
    return label;
  }
}
