export interface Area {
  id: number;
  name: string;
  location: string;
  description: string;
  imageUrl: string;
  latitude: number;
  longitude: number;
  createdAtUtc: string;
  todayVisitors: number;
  todayAppointments: number;
}

export interface AreasResponse {
  total: number;
  page: number;
  pageSize: number;
  items: Area[];
}