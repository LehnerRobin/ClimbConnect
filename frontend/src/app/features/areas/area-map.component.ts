import { Component, Input, OnChanges, OnDestroy, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { Router } from '@angular/router';
import * as L from 'leaflet';
import { Area } from '../../../services/areas.service';

// Leaflet sucht die Marker-Icons standardmäßig über einen relativen Pfad,
// der beim Angular-Build nicht mitkopiert wird. Deshalb zeigen wir explizit
// auf die Bilder, die wir über angular.json nach /leaflet kopieren lassen.
const defaultIcon = L.icon({
  iconUrl: '/leaflet/marker-icon.png',
  iconRetinaUrl: '/leaflet/marker-icon-2x.png',
  shadowUrl: '/leaflet/marker-shadow.png',
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41]
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
      const marker = L.marker([area.latitude!, area.longitude!], { icon: defaultIcon })
        .addTo(this.map)
        .bindPopup(`<strong>${area.name}</strong><br>${area.location ?? ''}`);

      marker.on('click', () => this.router.navigate(['/areas', area.id]));
      this.markers.push(marker);
    }

    const bounds = L.latLngBounds(areasWithCoords.map(a => [a.latitude!, a.longitude!] as [number, number]));
    this.map.fitBounds(bounds, { padding: [40, 40], maxZoom: 12 });
  }
}
