import 'dart:io';
import 'dart:async';
import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:geolocator/geolocator.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';
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
  List<LatLng> _walkingRoute = []; // רשימה לנקודות מסלול הליכה
  bool _isLoadingRoute = false; // מצב טעינת מסלול

  // Map loading state
  bool _mapLoaded = false;

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

          // קבלת מסלול הליכה כשיש מיקום נוכחי
          if (_currentPosition != null) {
            _getWalkingRoute();
          }
        }
      }
    }

    // Set default position and mark map as loaded
    Future.delayed(const Duration(milliseconds: 500), () {
      if (mounted) {
        setState(() {
          _mapLoaded = true;
          _loading = false;
        });

        // מיד להתחיל לנסות לקבל מסלול אם יש יעד
        if (_destination != null && _currentPosition != null) {
          _getWalkingRoute();
        }
      }
    });

    // Get location first - this will determine the initial map center
    if (!Platform.isWindows) {
      _determinePosition();
      _startLocationUpdates();
    } else {
      // On Windows, if we have destination, use it, otherwise use default
      if (mounted) {
        setState(() {
          _currentPosition = null;
          _error = 'מיקום לא זמין בפלטפורמה זו';
          _loading = false;
        });

        // אם יש יעד אבל אין מיקום נוכחי, התמקד על היעד
        if (_destination != null && _mapLoaded) {
          Future.delayed(const Duration(milliseconds: 200), () {
            if (mounted) {
              _mapController.move(_destination!, 16.0);
              print(
                "NavigationMapPage: Map centered on destination (Windows): $_destination",
              );
            }
          });
        }
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
          timeLimit: Duration(seconds: 15), // timeout של 15 שניות
        ),
      );

      setState(() {
        _currentPosition = LatLng(position.latitude, position.longitude);
        _error = null;
      });

      // התמקד על המיקום הנוכחי מיד
      _mapController.move(_currentPosition!, 16.0);
      print(
        "NavigationMapPage: Map centered on current location: $_currentPosition",
      );

      // קבלת מסלול הליכה אם יש יעד - המסלול יתאים את המפה אוטומטית
      if (_destination != null) {
        await _getWalkingRoute();
      }

      print(
        "NavigationMapPage: Position determined: ${position.latitude}, ${position.longitude}",
      );
    } catch (e) {
      print("NavigationMapPage: Error getting position: $e");
      setState(() {
        _error = 'לא הצלחנו לאתר את המיקום הנוכחי';
      });

      // אם לא הצלחנו לקבל מיקום נוכחי אבל יש יעד, התמקד על היעד
      if (_destination != null) {
        _mapController.move(_destination!, 16.0);
        print(
          "NavigationMapPage: Fallback - Map centered on destination: $_destination",
        );
      }
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

          // עדכון מסלול הליכה
          if (_destination != null) {
            if (_walkingRoute.isNotEmpty) {
              // אם יש מסלול, בדוק אם המיקום השתנה משמעותית
              final distanceFromRoute = Geolocator.distanceBetween(
                position.latitude,
                position.longitude,
                _walkingRoute.first.latitude,
                _walkingRoute.first.longitude,
              );

              // אם המיקום השתנה יותר מ-50 מטר, עדכן מסלול
              if (distanceFromRoute > 50) {
                _getWalkingRoute();
              }
            } else {
              // אם אין מסלול עדיין, נסה לקבל אותו
              _getWalkingRoute();
            }
          }

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
          width: 10,
          height: 10,
          child: Container(
            decoration: BoxDecoration(
              color: Colors.blue,
              shape: BoxShape.circle,
              border: Border.all(color: Colors.white, width: 1),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withOpacity(0.2),
                  blurRadius: 2,
                  spreadRadius: 0.5,
                ),
              ],
            ),
            child: const Icon(Icons.person, color: Colors.white, size: 6),
          ),
        ),
      );
    }

    // Destination marker
    if (_destination != null) {
      markers.add(
        Marker(
          point: _destination!,
          width: 10,
          height: 10,
          child: Container(
            decoration: BoxDecoration(
              color: Colors.red,
              shape: BoxShape.circle,
              border: Border.all(color: Colors.white, width: 1),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withOpacity(0.3),
                  blurRadius: 2,
                  spreadRadius: 0.5,
                ),
              ],
            ),
            child: const Icon(Icons.shield, color: Colors.white, size: 6),
          ),
        ),
      );
    }

    return markers;
  }

  List<Polyline> _buildPolylines() {
    List<Polyline> polylines = [];

    // Draw walking route if available
    if (_walkingRoute.isNotEmpty) {
      polylines.add(
        Polyline(points: _walkingRoute, color: Colors.blue, strokeWidth: 4.0),
      );
    } else if (_currentPosition != null && _destination != null) {
      // Fallback to direct line if no route available
      polylines.add(
        Polyline(
          points: [_currentPosition!, _destination!],
          color: Colors.blue.withOpacity(0.5),
          strokeWidth: 3.0,
        ),
      );
    }

    return polylines;
  }

  // קבלת מסלול הליכה מ-OSRM API
  Future<void> _getWalkingRoute() async {
    if (_currentPosition == null || _destination == null) return;

    setState(() {
      _isLoadingRoute = true;
    });

    try {
      final start = _currentPosition!;
      final end = _destination!;

      // שימוש ב-OSRM API לקבלת מסלול הליכה
      final url =
          'https://router.project-osrm.org/route/v1/foot/${start.longitude},${start.latitude};${end.longitude},${end.latitude}?overview=full&geometries=geojson';

      print("🚶 Getting walking route from OSRM: $url");

      final response = await http
          .get(Uri.parse(url))
          .timeout(const Duration(seconds: 10));

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);

        if (data['routes'] != null && data['routes'].isNotEmpty) {
          final route = data['routes'][0];
          final coordinates = route['geometry']['coordinates'] as List;

          // המרת הקואורדינטות לרשימת LatLng
          List<LatLng> routePoints =
              coordinates
                  .map(
                    (coord) => LatLng(coord[1].toDouble(), coord[0].toDouble()),
                  )
                  .toList();

          setState(() {
            _walkingRoute = routePoints;
            _isLoadingRoute = false;
          });

          print("✅ Walking route loaded with ${routePoints.length} points");

          // הזזת המפה להציג את כל המסלול
          if (routePoints.isNotEmpty) {
            _fitMapToRoute(routePoints);
          }
        } else {
          print("❌ No routes found in response");
          setState(() {
            _isLoadingRoute = false;
          });
          // אם לא מצליחים לקבל מסלול, לפחות להתאים את המפה להציג את שתי הנקודות
          if (_currentPosition != null && _destination != null) {
            _fitMapToRoute([_currentPosition!, _destination!]);

            // הצגת הודעה שמסלול הניווט הפנימי לא זמין אבל ניתן לעבור לניווט חיצוני
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(
                content: Text(
                  'מסלול הליכה לא זמין, השתמש בכפתור "ניווט למרחב מוגן" למעבר לאפליקציית ניווט',
                ),
                backgroundColor: Colors.orange,
                duration: Duration(seconds: 4),
              ),
            );
          }
        }
      } else {
        print("❌ Failed to get route: ${response.statusCode}");
        setState(() {
          _isLoadingRoute = false;
        });
        // אם לא מצליחים לקבל מסלול, לפחות להתאים את המפה להציג את שתי הנקודות
        if (_currentPosition != null && _destination != null) {
          _fitMapToRoute([_currentPosition!, _destination!]);

          // הצגת הודעה שמסלול הניווט הפנימי לא זמין אבל ניתן לעבור לניווט חיצוני
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text(
                'בעיה בשרת המסלולים, השתמש בכפתור "ניווט למרחב מוגן" למעבר לאפליקציית ניווט',
              ),
              backgroundColor: Colors.orange,
              duration: Duration(seconds: 4),
            ),
          );
        }
      }
    } catch (e) {
      print("❌ Error getting walking route: $e");
      setState(() {
        _isLoadingRoute = false;
      });
      // אם לא מצליחים לקבל מסלול, לפחות להתאים את המפה להציג את שתי הנקודות
      if (_currentPosition != null && _destination != null) {
        _fitMapToRoute([_currentPosition!, _destination!]);

        // הצגת הודעה שמסלול הניווט הפנימי לא זמין אבל ניתן לעבור לניווט חיצוני
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
              'שגיאה בקבלת מסלול, השתמש בכפתור "ניווט למרחב מוגן" למעבר לאפליקציית ניווט',
            ),
            backgroundColor: Colors.orange,
            duration: Duration(seconds: 4),
          ),
        );
      }
    }
  }

  // התאמת המפה להציג את כל המסלול
  void _fitMapToRoute(List<LatLng> routePoints) {
    if (routePoints.isEmpty) return;

    // חישוב גבולות המסלול
    double minLat = routePoints.first.latitude;
    double maxLat = routePoints.first.latitude;
    double minLng = routePoints.first.longitude;
    double maxLng = routePoints.first.longitude;

    for (final point in routePoints) {
      minLat = math.min(minLat, point.latitude);
      maxLat = math.max(maxLat, point.latitude);
      minLng = math.min(minLng, point.longitude);
      maxLng = math.max(maxLng, point.longitude);
    }

    // הוספת מרווח
    final latPadding = (maxLat - minLat) * 0.1;
    final lngPadding = (maxLng - minLng) * 0.1;

    // חישוב המרכז והזום
    final centerLat = (minLat + maxLat) / 2;
    final centerLng = (minLng + maxLng) / 2;
    final center = LatLng(centerLat, centerLng);

    // חישוב זום מתאים
    final distance = Geolocator.distanceBetween(
      minLat - latPadding,
      minLng - lngPadding,
      maxLat + latPadding,
      maxLng + lngPadding,
    );

    double zoom = 16.0;
    if (distance > 10000)
      zoom = 12.0;
    else if (distance > 5000)
      zoom = 13.0;
    else if (distance > 2000)
      zoom = 14.0;
    else if (distance > 1000)
      zoom = 15.0;

    // הזזת המפה
    _mapController.move(center, zoom);
  }

  void _openExternalNavigation() async {
    if (_destination == null) {
      print("NavigationMapPage: No destination set for external navigation");
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('בזמן אזעקה, הכפתור יפתח ניווט למרחב מוגן הקרוב'),
          backgroundColor: Colors.blue,
          duration: Duration(seconds: 3),
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
                  try {
                    // נסה קודם את אפליקציית Google Maps
                    final mapsApp =
                        'comgooglemaps://?daddr=$lat,$lng&directionsmode=walking';
                    if (await canLaunchUrl(Uri.parse(mapsApp))) {
                      await launchUrl(
                        Uri.parse(mapsApp),
                        mode: LaunchMode.externalApplication,
                      );
                      print("✅ Google Maps app opened successfully");
                    } else {
                      // אם האפליקציה לא מותקנת, פתח דרך הדפדפן
                      String mapsWeb =
                          'https://www.google.com/maps/dir/?api=1&destination=$lat,$lng&travelmode=walking';

                      // אם יש מיקום נוכחי, הוסף אותו כנקודת המוצא
                      if (_currentPosition != null) {
                        mapsWeb =
                            'https://www.google.com/maps/dir/${_currentPosition!.latitude},${_currentPosition!.longitude}/$lat,$lng';
                      }

                      if (await canLaunchUrl(Uri.parse(mapsWeb))) {
                        await launchUrl(
                          Uri.parse(mapsWeb),
                          mode: LaunchMode.externalApplication,
                        );
                        print("✅ Google Maps web opened successfully");
                      } else {
                        throw Exception('Cannot open Google Maps');
                      }
                    }
                  } catch (e) {
                    print("❌ Error opening Google Maps: $e");
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text(
                          'לא ניתן לפתוח את Google Maps. נסה שוב מאוחר יותר.',
                        ),
                        backgroundColor: Colors.orange,
                        duration: Duration(seconds: 3),
                      ),
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
                  try {
                    // נסה קודם את ה-URL של Waze באפליקציה
                    final wazeApp = 'waze://?ll=$lat,$lng&navigate=yes';
                    if (await canLaunchUrl(Uri.parse(wazeApp))) {
                      await launchUrl(
                        Uri.parse(wazeApp),
                        mode: LaunchMode.externalApplication,
                      );
                      print("✅ Waze app opened successfully");
                    } else {
                      // אם האפליקציה לא מותקנת, פתח דרך הדפדפן
                      final wazeWeb =
                          'https://www.waze.com/ul?ll=$lat,$lng&navigate=yes&zoom=17';
                      if (await canLaunchUrl(Uri.parse(wazeWeb))) {
                        await launchUrl(
                          Uri.parse(wazeWeb),
                          mode: LaunchMode.externalApplication,
                        );
                        print("✅ Waze web opened successfully");
                      } else {
                        throw Exception('Cannot open Waze');
                      }
                    }
                  } catch (e) {
                    print("❌ Error opening Waze: $e");
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text(
                          'לא ניתן לפתוח את Waze. ודא שהאפליקציה מותקנת.',
                        ),
                        backgroundColor: Colors.orange,
                        duration: Duration(seconds: 3),
                      ),
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
                    try {
                      // נסה קודם את אפליקציית Apple Maps
                      final appleMapsApp = 'maps://?daddr=$lat,$lng&dirflg=w';
                      if (await canLaunchUrl(Uri.parse(appleMapsApp))) {
                        await launchUrl(
                          Uri.parse(appleMapsApp),
                          mode: LaunchMode.externalApplication,
                        );
                        print("✅ Apple Maps app opened successfully");
                      } else {
                        // fallback לדפדפן
                        final appleMapsWeb =
                            'http://maps.apple.com/?daddr=$lat,$lng&dirflg=w';
                        if (await canLaunchUrl(Uri.parse(appleMapsWeb))) {
                          await launchUrl(
                            Uri.parse(appleMapsWeb),
                            mode: LaunchMode.externalApplication,
                          );
                          print("✅ Apple Maps web opened successfully");
                        } else {
                          throw Exception('Cannot open Apple Maps');
                        }
                      }
                    } catch (e) {
                      print("❌ Error opening Apple Maps: $e");
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(
                          content: Text(
                            'לא ניתן לפתוח את Apple Maps. נסה שוב מאוחר יותר.',
                          ),
                          backgroundColor: Colors.orange,
                          duration: Duration(seconds: 3),
                        ),
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

  // Get shelter address from shelter details
  String _getShelterAddress() {
    print("🏠 === GETTING SHELTER ADDRESS ===");
    print("📊 Shelter details: ${widget.shelterDetails}");

    if (widget.shelterDetails == null) {
      print("❌ No shelter details available");
      return 'כתובת לא זמינה';
    }

    // נסה לקבל את הכתובת מהנתונים שהתקבלו
    String? address =
        widget.shelterDetails!['address'] ??
        widget.shelterDetails!['Address'] ??
        widget.shelterDetails!['shelter_address'];

    String? name =
        widget.shelterDetails!['name'] ??
        widget.shelterDetails!['shelter_name'] ??
        widget.shelterDetails!['Name'];

    print("🔍 Found address: '$address'");
    print("🔍 Found name: '$name'");

    // אם יש כתובת, החזר אותה
    if (address != null && address.isNotEmpty) {
      print("✅ Using address: $address");
      return address;
    }

    // אם יש שם, החזר אותו
    if (name != null && name.isNotEmpty) {
      print("✅ Using name: $name");
      return name;
    }

    // אם יש קואורדינטות, הצג אותן
    final lat =
        widget.shelterDetails!['latitude'] ??
        widget.shelterDetails!['Latitude'];
    final lng =
        widget.shelterDetails!['longitude'] ??
        widget.shelterDetails!['Longitude'];

    if (lat != null && lng != null) {
      final coordinates =
          'קואורדינטות: ${lat.toString().substring(0, 7)}, ${lng.toString().substring(0, 7)}';
      print("✅ Using coordinates: $coordinates");
      return coordinates;
    }

    print("⚠️ No address info found, using default");
    return 'מרחב מוגן קרוב';
  }

  Widget _buildMapContent() {
    if (_loading) {
      return const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            CircularProgressIndicator(),
            SizedBox(height: 16),
            Text('מאתר את המיקום שלך...', style: TextStyle(fontSize: 16)),
            SizedBox(height: 8),
            Text(
              'המפה תפתח במיקום הנוכחי שלך',
              style: TextStyle(fontSize: 14, color: Colors.grey),
            ),
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

              // Logo only
              Image.asset(
                'assets/images/LOGO.png',
                height: 80,
                width: 80,
                errorBuilder: (context, error, stackTrace) {
                  return Container(
                    height: 80,
                    width: 80,
                    decoration: BoxDecoration(
                      color: Colors.red,
                      borderRadius: BorderRadius.circular(16),
                    ),
                    child: const Icon(
                      Icons.home,
                      color: Colors.white,
                      size: 40,
                    ),
                  );
                },
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
                    borderRadius: BorderRadius.circular(10),
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

        // כותרת ממורכזת עם כתובת המרחב המוגן
        if (_destination != null)
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Column(
              children: [
                const Text(
                  'נמצא עבורך מרחב מוגן בכתובת:',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: Color.fromARGB(255, 29, 46, 89),
                  ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 4),
                Text(
                  _getShelterAddress(),
                  style: const TextStyle(fontSize: 14, color: Colors.black87),
                  textAlign: TextAlign.center,
                ),
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
              borderRadius: BorderRadius.circular(10),
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
              borderRadius: BorderRadius.circular(10),
              child: Stack(
                children: [
                  // Flutter Map
                  FlutterMap(
                    mapController: _mapController,
                    options: MapOptions(
                      initialCenter:
                          _currentPosition ??
                          _destination ?? // אם אין מיקום נוכחי, נסה היעד
                          const LatLng(
                            32.0853,
                            34.7818,
                          ), // ברירת מחדל רק כ-fallback אחרון
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
                                  ? const Color.fromARGB(255, 29, 46, 89)
                                  : const Color.fromARGB(0, 158, 158, 158),
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

                  // Loading route indicator
                  if (_isLoadingRoute)
                    Positioned(
                      top: 80,
                      left: 20,
                      right: 20,
                      child: Container(
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: Colors.blue,
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: const Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            SizedBox(
                              width: 20,
                              height: 20,
                              child: CircularProgressIndicator(
                                color: Colors.white,
                                strokeWidth: 2,
                              ),
                            ),
                            SizedBox(width: 8),
                            Text(
                              'טוען מסלול הליכה...',
                              style: TextStyle(color: Colors.white),
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
              // External Navigation button - תמיד פעיל אם יש יעד
              Expanded(
                child: ElevatedButton.icon(
                  onPressed:
                      _destination != null ? _openExternalNavigation : null,
                  icon: Icon(
                    Icons.navigation,
                    color:
                        _destination != null ? Colors.white : Colors.grey[400],
                  ),
                  label: Text(
                    _destination != null ? 'ניווט למרחב מוגן' : 'אין יעד זמין',
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: Colors.white,
                    ),
                  ),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color.fromARGB(255, 29, 46, 89),
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 12),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(10),
                    ),
                    elevation: _destination != null ? 6 : 2,
                  ),
                ),
              ),

              // רווח בין הכפתורים
              const SizedBox(width: 16),

              // "Arrived safely" button
              Expanded(
                child: ElevatedButton.icon(
                  onPressed: _goToArrivalSuccessPage,
                  icon: const Icon(Icons.check_circle, color: Colors.white),
                  label: const Text(
                    'הגעת בשלום?',
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: Colors.white,
                    ),
                  ),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color.fromARGB(255, 29, 46, 89),
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 12),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(10),
                    ),
                    elevation: 6,
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
