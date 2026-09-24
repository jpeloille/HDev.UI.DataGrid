# Plan d'amélioration — HDevDataGrid vers le niveau DevExpress

> Objectif : amener `HDev.UI.DataGrid` au niveau de fonctionnalités, de
> performance et de finition d'un `GridControl` DevExpress. Ce document liste
> **toutes** les améliorations proposées, classées par thème, puis priorisées en
> phases livrables.

---

## 0. État actuel (constat technique réel)

| Domaine | État réel | Écart vs DevExpress |
|---|---|---|
| Virtualisation lignes | OK (`VirtualizingStackPanel`) | Pas de recyclage des cellules, pas de hauteur variable |
| Virtualisation colonnes | ❌ absente (`StackPanel` horizontal) | Lent si beaucoup de colonnes |
| Édition | ❌ stub (`TODO` dans `BeginEdit`/`CancelEdit`) | Pas d'éditeurs, pas de validation, pas de new-row |
| Grouping | ⚠️ moteur OK, **rendu absent** | Les group rows ne s'affichent pas dans le scroll |
| Accès données | Réflexion à chaque accès | Pas de délégués compilés / cache |
| Filtre | `Contains` codé en dur | Pas d'opérateurs, pas de filtre Excel, pas de filter builder |
| Summaries / totaux | ❌ | Aucun agrégat |
| Export / impression | ❌ | — |
| Master-detail | ❌ | — |
| Sélection | Lignes surtout | Pas de plage de cellules, pas de copier/coller |
| Accessibilité | ❌ pas d'AutomationPeer | — |
| Thème | 1 thème clair | Pas de dark / densité / tokens |

---

## 1. Socle performance & architecture (fondations — à faire en premier)

Ces points conditionnent tout le reste : inutile d'ajouter des features sur une
base qui réfléchit à chaque cellule.

1.1. **Accès aux propriétés par délégués compilés**
   - Remplacer `PropertyInfo.GetValue/SetValue` (dans `GridColumn`, `GridDataSource`,
     `ApplySorting`, `FilterGroup.Matches`) par des `Func<object,object?>` /
     `Action<object,object?>` compilés via `Expression` et mis en cache par
     `(Type, FieldName)`.
   - Support des chemins imbriqués (`Customer.Address.City`).
   - Impact : x10–x50 sur tri/filtre de gros volumes.

1.2. **Moteur de données séparé de la vue** (`GridDataController`)
   - Isoler tri/filtre/groupe/agrégats dans une couche testable, sans dépendance UI.
   - Pipeline incrémental : ne recalculer que ce qui change (filtre seul ≠ re-tri complet).
   - Option de calcul en arrière-plan (async) avec annulation pour > 100k lignes.

1.3. **Virtualisation des colonnes + recyclage de cellules**
   - ✅ **Recyclage fait** : `HDevDataGridRow` ne reconstruit plus toutes ses cellules à
     chaque recyclage de ligne (changement de DataContext au scroll) ; les cellules
     re-bindent `RowData` et réutilisent les mêmes instances. Filet `RefreshCells`
     pour refléter les changements structurels (réordre/freeze/visibilité) sur les
     lignes déjà rendues. Vérifié dans `tests/HeadlessSmoke` (recycle = même instance,
     valeur mise à jour).
   - ⏳ **Virtualisation horizontale vraie = projet séparé.** Mesure (headless,
     indicative) : le coût du layout initial croît linéairement avec le nombre de
     colonnes (10→106ms/150 cellules, 60→474ms/900, 100→557ms/1500) car toutes les
     colonnes sont matérialisées. Contrainte d'archi : le scroll horizontal appartient
     au `ScrollViewer` externe, donc un `VirtualizingStackPanel` par ligne ne
     virtualiserait rien (lignes mesurées en largeur infinie). Une vraie virtualisation
     exige un **viewport horizontal partagé** piloté par l'offset de la grille —
     redesign profond (colonnes figées, synchro d'en-tête, resize). À n'entreprendre
     que si des grilles à 60+ colonnes saccadent sur un vrai écran après le fix de
     recyclage. Sinon, rester sur many-rows (cas commun, déjà géré).

1.4. **Modèle de lignes unifié (rows + group rows + summary rows)**
   - Une seule liste virtualisée de « visual rows » (data / group-header / group-summary /
     total-summary / new-row), pour que le grouping et les totaux s'affichent *dans* le scroll.
   - C'est le refactor clé qui débloque grouping rendu + summaries + master-detail.

1.5. **Hauteur de ligne variable & word-wrap**
   - Auto-height par contenu, retour à la ligne, mesure mise en cache.

---

## 2. Édition (actuellement absente)

2.1. **Système d'éditeurs in-place** par `ColumnType` + registre extensible :
   TextBox, NumericUpDown (masques), DatePicker/TimePicker, CheckBox, ComboBox
   (enum/lookup), AutoComplete, masque/regex.
2.2. **Cycle d'édition réel** : `BeginEdit` (swap template), `CommitEdit`,
   `CancelEdit` avec **restauration de la valeur d'origine** (clone/rollback).
2.3. **Validation** : `INotifyDataErrorInfo` + `IDataErrorInfo` + règles par colonne,
   feedback visuel (bordure rouge, tooltip), blocage du commit si invalide.
2.4. **Conversion typée robuste** : remplacer `Convert.ChangeType` (échoue sur enum,
   nullable, culture) par un convertisseur tenant compte de la culture et du type cible.
2.5. **New Item Row** (ligne d'ajout en haut/bas) et **suppression** (Del).
2.6. **Lookup columns** (affiche libellé, stocke clé — équivalent `LookUpEdit`).

---

## 3. Grouping (moteur OK, rendu et finition à faire)

3.1. **Rendu des group rows dans le scroll** (via §1.4).
3.2. **Group-by box fonctionnel** : drag d'en-tête vers le panneau, multi-niveaux,
   réordonnancement des niveaux, retrait.
3.3. **Group summaries** : agrégats par groupe affichés dans l'en-tête de groupe.
3.4. **Sticky group headers** (en-tête de groupe collant en haut au scroll).
3.5. **Expand/collapse animé**, état mémorisé, « expand to level N ».
3.6. **Custom group intervals** (par date : jour/mois/année ; par plage numérique ;
   alphabétique) — équivalent `GroupInterval`.

---

## 4. Summaries / agrégats (absent)

4.1. ✅ **Footer total summary** : Count/Sum/Average/Min/Max par colonne, format +
   caption, aligné sous les colonnes et synchronisé au scroll horizontal. Propriétés
   `ShowSummaryFooter` + collection `TotalSummaries`. Vérifié dans `tests/HeadlessSmoke`
   (moteur + rendu). Reste : agrégat Custom (délégué).
4.2. ✅ **Group summaries** : agrégats par groupe affichés dans l'en-tête de groupe
   (collection `GroupSummaries`), calculés sur les items de chaque groupe. Vérifié
   headless (somme par département).
4.3. **Summary sur sélection** (total des cellules sélectionnées, comme Excel).
4.4. **Format & alignement** des résultats d'agrégat.

---

## 5. Filtrage (très limité aujourd'hui)

5.1. **Filtre ligne par-colonne avec opérateurs** (pas seulement `Contains`) :
   menu d'opérateur par type (texte/nombre/date/bool).
5.2. **Excel-style filter dropdown** : checklist des valeurs distinctes + recherche.
5.3. **Filter Editor / Filter Builder** (criteria tree AND/OR imbriqués).
5.4. **Filter panel** en bas : affiche le critère actif, bouton effacer/éditer, toggle on/off.
5.5. **Auto-filter debounce** + filtre insensible accents/casse paramétrable.

---

## 6. Recherche

6.1. **Search panel** (barre de recherche globale) avec surlignage des correspondances
   et navigation suivant/précédent. Équivalent `FindPanel` DevExpress.

---

## 7. Colonnes — fonctions avancées

7.1. **Column chooser** (popup des colonnes masquées, drag pour montrer/cacher).
7.2. **Bandes / en-têtes multi-lignes** (`Band columns`) — regroupement visuel de colonnes.
7.3. **Frozen columns gauche ET droite** (aujourd'hui : lock de position seulement).
7.4. **Best-fit width** (double-clic sur le séparateur = ajuste à la largeur du contenu),
   et best-fit all.
7.5. **Auto-width modes** : étoile, contenu, fixe, en respectant min/max.
7.6. **Cell merging** (fusion verticale de cellules identiques).
7.7. **Header context menu** (tri, group by, masquer, best fit, chooser…).
7.8. **Unbound/calculated columns** (valeur calculée par expression/délégué).

---

## 8. Sélection & presse-papier

8.1. **Sélection de plage de cellules** (cell range, comme Excel) + sélection multiple.
8.2. **Navigation clavier complète** : flèches gauche/droite entre cellules, Tab/Shift+Tab,
   Ctrl+flèches, navigation cellule courante (aujourd'hui : vertical seulement).
8.3. **Copier (Ctrl+C)** lignes/cellules au presse-papier (texte tabulé + HTML).
8.4. **Coller (Ctrl+V)** dans cellules éditables.
8.5. **Cell focus / current cell** visuel distinct de la sélection de ligne.

---

## 9. Données — scénarios serveur & grands volumes

9.1. **Pagination** (UI pager + `PageSize`).
9.2. **Infinite scrolling / lazy loading** (charge au scroll).
9.3. **Server-side sort/filter/group** via interface `IGridDataSource` async
   (le grid délègue le calcul au backend).
9.4. **Virtual data source** (fenêtre de données, total connu, fetch à la demande).

---

## 10. Master-detail & hiérarchie

10.1. **Detail grids** (ligne expandable révélant un sous-grid / vue détail).
10.2. **Self-referencing tree** (TreeList : hiérarchie parent/enfant dans la même grille).

---

## 11. Mise en forme conditionnelle

11.1. **Format conditions** : règles (>, <, between, top N, data bar, color scale,
   icon set, expression) appliquant style/couleur/icône aux cellules.
11.2. **Style par ligne/cellule via délégué** (`RowStyle`, `CellStyle` callbacks).

---

## 12. Export & impression

12.1. **Export Excel (.xlsx)** avec mise en forme, groupes, summaries.
12.2. **Export CSV** (séparateur/culture paramétrables).
12.3. **Export PDF**.
12.4. **Impression / aperçu** (pagination, en-têtes répétés).

---

## 13. Persistance d'état (layout)

13.1. **Save/Restore layout** : ordre, largeur, visibilité, tri, filtre, groupes,
   colonnes figées → JSON/stream. Essentiel pour une grille « pro ».

---

## 14. Interactions

14.1. **Row drag & drop** (réordonner, déplacer entre grilles).
14.2. **Tooltips de cellule** (contenu tronqué) et tooltips custom.
14.3. **Context menu** lignes/cellules configurable.

---

## 15. Accessibilité, i18n, thèmes

15.1. **AutomationPeers** (lecteurs d'écran, tests UI).
15.2. **Navigation 100% clavier** (cf. §8.2) + raccourcis configurables.
15.3. **RTL**.
15.4. **Localisation** des textes intégrés (filtres, menus, « no data »…).
15.5. **Thèmes** : design tokens, **dark mode**, densité (compact/normal/spacieux),
   variantes Fluent, surfaces de styling documentées.

---

## 16. Qualité / ingénierie

16.1. **Tests unitaires** du moteur (tri/filtre/groupe/agrégat) + tests UI headless Avalonia.
16.2. **Benchmarks** (BenchmarkDotNet) pour valider les chiffres du README (actuellement non mesurés).
16.3. **Documentation API** + samples par feature dans la DemoApp.
16.4. **Packaging NuGet** propre (multi-target, symboles, doc XML).

---

## Roadmap priorisée

### Phase 1 — Fondations (débloque tout le reste)
- §1.1 Délégués compilés + cache
- §1.4 Modèle de visual rows unifié
- §1.3 Virtualisation/recyclage colonnes
- §3.1 Rendu des group rows
- §2.1–2.3 Édition réelle + validation

### Phase 2 — Parité fonctionnelle « pro »
- §4 Summaries (footer + groupe)
- §5 Filtrage avancé (opérateurs + Excel filter + filter panel)
- §6 Search panel
- §7.1 Column chooser, §7.4 best-fit, §7.7 header menu
- §8 Sélection cellules + copier/coller + navigation clavier
- §13 Persistance layout

### Phase 3 — Différenciateurs DevExpress
- §10 Master-detail / TreeList
- §11 Conditional formatting
- §12 Export Excel/CSV/PDF + impression
- §7.2 Bandes d'en-têtes
- §9 Scénarios serveur (paging, lazy, server-side)

### Phase 4 — Finition
- §15 Accessibilité, dark mode, densité, RTL, i18n
- §14 Drag & drop, tooltips, context menus
- §16 Tests, benchmarks, doc, NuGet

---

## Premier chantier recommandé

Commencer par **§1.1 (délégués compilés)** + **§1.4 (visual rows) + §3.1 (rendu groupes)**
et **§2 (édition)** : c'est le socle qui rend la grille crédible et débloque
summaries, master-detail et conditional formatting derrière.
