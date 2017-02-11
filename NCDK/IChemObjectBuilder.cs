using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using NCDK.Numerics;

namespace NCDK
{
    public interface IChemObjectBuilder
    {
        IAdductFormula CreateAdductFormula();
        IAdductFormula CreateAdductFormula(IMolecularFormula formula);
        IAminoAcid CreateAminoAcid();
        IAtom CreateAtom();
        IAtom CreateAtom(IElement element);
        IAtom CreateAtom(string elementSymbol);
        IAtom CreateAtom(string elementSymbol, Vector2 point2d);
        IAtom CreateAtom(string elementSymbol, Vector3 point3d);
        IAtomContainerSet<T> CreateAtomContainerSet<T>() where T : IAtomContainer;
        IAtomContainer CreateAtomContainer();
        IAtomContainer CreateAtomContainer(IAtomContainer container);
        IAtomContainer CreateAtomContainer(IEnumerable<IAtom> atoms, IEnumerable<IBond> bonds);
        IAtomContainerSet<IAtomContainer> CreateAtomContainerSet();
        IAtomType CreateAtomType(IElement element);
        IAtomType CreateAtomType(string elementSymbol);
        IAtomType CreateAtomType(string identifier, string elementSymbol);
        IBioPolymer CreateBioPolymer();
        IBond CreateBond();
        IBond CreateBond(IAtom atom1, IAtom atom2);
        IBond CreateBond(IAtom atom1, IAtom atom2, BondOrder order);
        IBond CreateBond(IEnumerable<IAtom> atoms);
        IBond CreateBond(IEnumerable<IAtom> atoms, BondOrder order);
        IBond CreateBond(IAtom atom1, IAtom atom2, BondOrder order, BondStereo stereo);
        IBond CreateBond(IEnumerable<IAtom> atoms, BondOrder order, BondStereo stereo);
        IChemFile CreateChemFile();
        IChemModel CreateChemModel();
        IChemObject CreateChemObject();
        IChemObject CreateChemObject(IChemObject chemObject);
        IChemSequence CreateChemSequence();
        ICrystal CreateCrystal();
        ICrystal CreateCrystal(IAtomContainer container);
        IElectronContainer CreateElectronContainer();
        IElement CreateElement();
        IElement CreateElement(IElement element);
        IElement CreateElement(string symbol);
        IElement CreateElement(string symbol, int? atomicNumber);
        IFragmentAtom CreateFragmentAtom();
        ILonePair CreateLonePair();
        ILonePair CreateLonePair(IAtom atom);
        IIsotope CreateIsotope(string elementSymbol);
        IIsotope CreateIsotope(int atomicNumber, string elementSymbol, int massNumber, double exactMass, double abundance);
        IIsotope CreateIsotope(int atomicNumber, string elementSymbol, double exactMass, double abundance);
        IIsotope CreateIsotope(string elementSymbol, int massNumber);
        IIsotope CreateIsotope(IElement element);
        IMapping CreateMapping(IChemObject objectOne, IChemObject objectTwo);
        IMolecularFormula CreateMolecularFormula();
        IMolecularFormulaSet CreateMolecularFormulaSet();
        IMolecularFormulaSet CreateMolecularFormulaSet(IMolecularFormula formula);
        IMonomer CreateMonomer();
        IPseudoAtom CreatePseudoAtom();
        IPseudoAtom CreatePseudoAtom(string label);
        IPseudoAtom CreatePseudoAtom(IElement element);
        IPseudoAtom CreatePseudoAtom(string label, Vector2 point2d);
        IPseudoAtom CreatePseudoAtom(string label, Vector3 point3d);
        IReaction CreateReaction();
        IReactionSet CreateReactionSet();
        IReactionScheme CreateReactionScheme();
        IPDBAtom CreatePDBAtom(IElement element);
        IPDBAtom CreatePDBAtom(string symbol);
        IPDBAtom CreatePDBAtom(string symbol, Vector3 coordinate);
        IPDBMonomer CreatePDBMonomer();
        IPDBPolymer CreatePDBPolymer();
        IPDBStructure CreatePDBStructure();
        IPolymer CreatePolymer();
        IRing CreateRing();
        IRing CreateRing(int ringSize, string elementSymbol);
        IRing CreateRing(IAtomContainer atomContainer);
        IRing CreateRing(IEnumerable<IAtom> atoms, IEnumerable<IBond> bonds);
        IRingSet CreateRingSet();
        ISubstance CreateSubstance();
        ISingleElectron CreateSingleElectron();
        ISingleElectron CreateSingleElectron(IAtom atom);
        IStrand CreateStrand();
        ITetrahedralChirality CreateTetrahedralChirality(IAtom chiralAtom, IEnumerable<IAtom> ligandAtoms, TetrahedralStereo chirality);
    }
}
