import 'dart:io';
import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:geolocator/geolocator.dart';
import 'package:url_launcher/url_launcher.dart';
import 'base_page_emergency.dart';
import 'arrival_success_page.dart';

class NavigationMapPage extends StatefulWidget {
  final Map<String, dynamic>? shelterDetails;
  final Map<String, dynamic>? routeInfo;

  const NavigationMapPage({Key? key, this.shelterDetails, this.routeInfo})
    : super(key: key);

  @override
  State<NavigationMapPage> createState() => _NavigationMapPageState();
}

class _NavigationMapPageState extends State<NavigationMapPage> {
  final MapController _mapController = MapController();
  LatLng? _currentPosition;
  bool _loading = true;
  String? _error;
  LatLng? _destination;
  StreamSubscription<Position>? _positionStreamSubscription;

  // Map loading state
  bool _mapLoaded = false;
  bool _initialLocationSet = false;

  @override
  void initState() {
    super.initState();
    print("NavigationMapPage: initState started");
    _initializeLocation();

    // Parse shelter details if provided
    if (widget.shelterDetails != null) {
      final lat =
          widget.shelterDetails!['Latitude'] ??
          widget.shelterDetails!['latitude'];
      final lng =
          widget.shelterDetails!['Longitude'] ??
          widget.shelterDetails!['longitude'];

      if (lat != null && lng != null) {
        final parsedLat = double.tryParse(lat.toString());
        final parsedLng = double.tryParse(lng.toString());

        if (parsedLat != null && parsedLng != null) {
          _destination = LatLng(parsedLat, parsedLng);
          print("NavigationMapPage: Using shelter destination: $_destination");
        }
      }
    }

    // Set default position (Tel Aviv) and mark map as loaded
    Future.delayed(const Duration(milliseconds: 500), () {
      if (mounted) {
        setState(() {
          _mapLoaded = true;
          _loading = false;
        });
      }
    });

    // Get location only if not Windows
    if (!Platform.isWindows) {
      _determinePosition();
      _startLocationUpdates();
    } else {
      if (mounted) {
        setState(() {
          _currentPosition = null;
          _error = 'מיקום לא זמין בפלטפורמה זו';
          _loading = false;
        });
      }
    }
  }

  void _initializeLocation() async {
    try {
      // Check if location services are enabled
      bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) {
        setState(() {
          _error = 'שירותי מיקום כבויים';
          _loading = false;
        });
        return;
      }

      // Check permissions
      LocationPermission permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
        if (permission == LocationPermission.denied) {
          setState(() {
            _error = 'הרשאות מיקום נדחו';
            _loading = false;
          });
          return;
        }
      }

      if (permission == LocationPermission.deniedForever) {
        setState(() {
          _error = 'הרשאות מיקום נדחו לצמיתות';
          _loading = false;
        });
        return;
      }

      setState(() {
        _loading = false;
      });
    } catch (e) {
      setState(() {
        _error = 'שגיאה באתחול מיקום: $e';
        _loading = false;
      });
    }
  }

  Future<void> _determinePosition() async {
    try {
      Position position = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
        ),
      );

      setState(() {
        _currentPosition = LatLng(position.latitude, position.longitude);
        _error = null;
      });

      // Move map to current location on first determination
      if (_mapLoaded && _currentPosition != null && !_initialLocationSet) {
        _mapController.move(_currentPosition!, 16.0);
        _initialLocationSet = true;
      }

      print(
        "NavigationMapPage: Position determined: ${position.latitude}, ${position.longitude}",
      );
    } catch (e) {
      print("NavigationMapPage: Error getting position: $e");
      setState(() {
        _error = 'שגיאה בקבלת מיקום: $e';
      });
    }
  }

  void _startLocationUpdates() {
    if (Platform.isWindows) return;

    _positionStreamSubscription = Geolocator.getPositionStream(
      locationSettings: const LocationSettings(
        accuracy: LocationAccuracy.high,
        distanceFilter: 10,
      ),
    ).listen(
      (Position position) {
        if (mounted) {
          setState(() {
            _currentPosition = LatLng(position.latitude, position.longitude);
            _error = null;
          });

          // Don't auto-move map after initial positioning to avoid interrupting user navigation
        }
      },
      onError: (error) {
        print("NavigationMapPage: Location stream error: $error");
        if (mounted) {
          setState(() {
            _error = 'שגיאה בעדכון מיקום: $error';
          });
        }
      },
    );
  }

  @override
  void dispose() {
    _positionStreamSubscription?.cancel();
    super.dispose();
  }

  List<Marker> _buildMarkers() {
    List<Marker> markers = [];

    // Current location marker
    if (_currentPosition != null) {
      markers.add(
        Marker(
          point: _currentPosition!,
          child: const Icon(Icons.my_location, color: Colors.blue, size: 30),
        ),
      );
    }

    // Destination marker
    if (_destination != null) {
      markers.add(
        Marker(
          point: _destination!,
          child: const Icon(Icons.location_pin, color: Colors.red, size: 40),
        ),
      );
    }

    return markers;
  }

  List<Polyline> _buildPolylines() {
    List<Polyline> polylines = [];

    // Draw line from current position to destination
    if (_currentPosition != null && _destination != null) {
      polylines.add(
        Polyline(
          points: [_currentPosition!, _destination!],
          color: Colors.blue,
          strokeWidth: 5.0,
        ),
      );
    }

    return polylines;
  }

  void _openExternalNavigation() async {
    if (_destination == null) {
      print("NavigationMapPage: No destination set for external navigation");
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('יש לבחור מרחב מוגן תחילה כדי לנווט אליו'),
          backgroundColor: Colors.orange,
        ),
      );
      return;
    }

    final double lat = _destination!.latitude;
    final double lng = _destination!.longitude;

    // Show options for external navigation apps
    showModalBottomSheet(
      context: context,
      builder: (BuildContext context) {
        return Container(
          padding: const EdgeInsets.all(20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text(
                'בחר אפליקציית ניווט',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 20),

              // Google Maps
              ListTile(
                leading: const Icon(Icons.map, color: Colors.green),
                title: const Text('Google Maps'),
                onTap: () async {
                  Navigator.pop(context);
                  final url =
                      'https://www.google.com/maps/dir/?api=1&destination=$lat,$lng';
                  if (await canLaunchUrl(Uri.parse(url))) {
                    await launchUrl(
                      Uri.parse(url),
                      mode: LaunchMode.externalApplication,
                    );
                  }
                },
              ),

              // Waze
              ListTile(
                leading: const Icon(Icons.navigation, color: Colors.blue),
                title: const Text('Waze'),
                onTap: () async {
                  Navigator.pop(context);
                  final url = 'https://waze.com/ul?ll=$lat,$lng&navigate=yes';
                  if (await canLaunchUrl(Uri.parse(url))) {
                    await launchUrl(
                      Uri.parse(url),
                      mode: LaunchMode.externalApplication,
                    );
                  }
                },
              ),

              // Apple Maps (iOS only)
              if (Platform.isIOS)
                ListTile(
                  leading: const Icon(Icons.map_outlined, color: Colors.grey),
                  title: const Text('Apple Maps'),
                  onTap: () async {
                    Navigator.pop(context);
                    final url = 'http://maps.apple.com/?daddr=$lat,$lng';
                    if (await canLaunchUrl(Uri.parse(url))) {
                      await launchUrl(
                        Uri.parse(url),
                        mode: LaunchMode.externalApplication,
                      );
                    }
                  },
                ),
            ],
          ),
        );
      },
    );
  }

  void _goToArrivalSuccessPage() {
    Navigator.of(
      context,
    ).push(MaterialPageRoute(builder: (context) => const ArrivalSuccessPage()));
  }

  void _centerOnCurrentLocation() {
    if (_currentPosition != null) {
      _mapController.move(_currentPosition!, 16.0);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('התמקדתי במיקום הנוכחי'),
          duration: Duration(seconds: 2),
        ),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('מיקום נוכחי לא זמין'),
          backgroundColor: Colors.orange,
          duration: Duration(seconds: 2),
        ),
      );
    }
  }

  // Calculate distance between current position and destination
  String _calculateDistance() {
    if (_currentPosition == null || _destination == null) {
      return '';
    }

    double distanceInMeters = Geolocator.distanceBetween(
      _currentPosition!.latitude,
      _currentPosition!.longitude,
      _destination!.latitude,
      _destination!.longitude,
    );

    if (distanceInMeters < 1000) {
      return '${distanceInMeters.round()} מ\'';
    } else {
      double distanceInKm = distanceInMeters / 1000;
      return '${distanceInKm.toStringAsFixed(1)} ק"מ';
    }
  }

  Widget _buildMapContent() {
    if (_loading) {
      return const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            CircularProgressIndicator(),
            SizedBox(height: 16),
            Text('טוען מפה...', style: TextStyle(fontSize: 16)),
          ],
        ),
      );
    }

    return Column(
      children: [
        // Header with logo and distance info
        Container(
          padding: const EdgeInsets.fromLTRB(16, 20, 16, 10),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              // Back button
              Container(
                decoration: BoxDecoration(
                  color: Colors.white,
                  shape: BoxShape.circle,
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withOpacity(0.1),
                      spreadRadius: 1,
                      blurRadius: 4,
                      offset: const Offset(0, 2),
                    ),
                  ],
                ),
                child: IconButton(
                  onPressed: () => Navigator.of(context).pop(),
                  icon: const Icon(Icons.arrow_forward, color: Colors.black),
                ),
              ),

              // Logo and app name
              Column(
                children: [
                  Image.asset(
                    'assets/images/LOGO.png',
                    height: 40,
                    width: 40,
                    errorBuilder: (context, error, stackTrace) {
                      return Container(
                        height: 40,
                        width: 40,
                        decoration: BoxDecoration(
                          color: Colors.red,
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: const Icon(Icons.home, color: Colors.white),
                      );
                    },
                  ),
                  const SizedBox(height: 4),
                  const Text(
                    'FIND COVER',
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: Colors.black87,
                    ),
                  ),
                ],
              ),

              // Distance info
              if (_destination != null)
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 6,
                  ),
                  decoration: BoxDecoration(
                    color: Colors.green,
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Column(
                    children: [
                      const Text(
                        'מרחק ליעד:',
                        style: TextStyle(fontSize: 10, color: Colors.white),
                      ),
                      Text(
                        _calculateDistance(),
                        style: const TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.bold,
                          color: Colors.white,
                        ),
                      ),
                    ],
                  ),
                )
              else
                Container(width: 60), // Placeholder to maintain layout
            ],
          ),
        ),

        // Map container with border and rounded corners
        Expanded(
          flex: 4, // Adjusted for header space
          child: Container(
            margin: const EdgeInsets.fromLTRB(
              16,
              16,
              16,
              8,
            ), // Reduced bottom margin
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: Colors.grey[300]!, width: 2),
              boxShadow: [
                BoxShadow(
                  color: Colors.grey.withOpacity(0.5),
                  spreadRadius: 2,
                  blurRadius: 8,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            child: ClipRRect(
              borderRadius: BorderRadius.circular(18),
              child: Stack(
                children: [
                  // Flutter Map
                  FlutterMap(
                    mapController: _mapController,
                    options: MapOptions(
                      initialCenter:
                          _currentPosition ??
                          const LatLng(
                            32.0853,
                            34.7818,
                          ), // Current location priority
                      initialZoom: 16.0,
                      minZoom: 5.0,
                      maxZoom: 18.0,
                    ),
                    children: [
                      // Map tiles
                      TileLayer(
                        urlTemplate:
                            'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                        userAgentPackageName: 'com.example.find_cover',
                      ),
                      // Polylines (routes)
                      PolylineLayer(polylines: _buildPolylines()),
                      // Markers
                      MarkerLayer(markers: _buildMarkers()),
                    ],
                  ),

                  // Current location button (floating)
                  Positioned(
                    top: 20,
                    right: 20,
                    child: Container(
                      decoration: BoxDecoration(
                        color: Colors.white,
                        shape: BoxShape.circle,
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withOpacity(0.2),
                            spreadRadius: 1,
                            blurRadius: 4,
                            offset: const Offset(0, 2),
                          ),
                        ],
                      ),
                      child: IconButton(
                        onPressed: _centerOnCurrentLocation,
                        icon: Icon(
                          Icons.my_location,
                          color:
                              _currentPosition != null
                                  ? Colors.blue
                                  : Colors.grey,
                        ),
                      ),
                    ),
                  ),

                  // Error message overlay
                  if (_error != null)
                    Positioned(
                      top: 20,
                      left: 20,
                      right: 80, // Leave space for location button
                      child: Container(
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: Colors.orange[100],
                          border: Border.all(color: Colors.orange),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Row(
                          children: [
                            Icon(Icons.warning, color: Colors.orange[800]),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Text(
                                _error!,
                                style: TextStyle(color: Colors.orange[800]),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                ],
              ),
            ),
          ),
        ),

        // Navigation buttons at the bottom - always show
        Container(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 20),
          child: Row(
            children: [
              // External Navigation button
              Expanded(
                child: Container(
                  margin: const EdgeInsets.only(right: 8),
                  child: ElevatedButton.icon(
                    onPressed:
                        _destination != null ? _openExternalNavigation : null,
                    icon: const Icon(Icons.navigation, color: Colors.white),
                    label: const Text(
                      'ניווט למרחב מוגן',
                      style: TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: Colors.white,
                      ),
                    ),
                    style: ElevatedButton.styleFrom(
                      backgroundColor:
                          _destination != null
                              ? Colors.blue[600]
                              : Colors.grey[400],
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(vertical: 12),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(25),
                      ),
                      elevation: 6,
                    ),
                  ),
                ),
              ),

              // "Arrived safely" button
              Expanded(
                child: Container(
                  margin: const EdgeInsets.only(left: 8),
                  child: ElevatedButton.icon(
                    onPressed: _goToArrivalSuccessPage,
                    icon: const Icon(Icons.check_circle, color: Colors.white),
                    label: const Text(
                      'הגעתי בשלום?',
                      style: TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: Colors.white,
                      ),
                    ),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.green[600],
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(vertical: 12),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(25),
                      ),
                      elevation: 6,
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    return BasePage(child: _buildMapContent());
  }
}
